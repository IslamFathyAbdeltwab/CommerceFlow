using System.Text;
using System.Text.Json;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Notifications.Commands.MarkNotificationSent;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommerceFlow.Infrastructure.Notifications;

// The delivery half of the Notifications story. RabbitNotificationQueue hands
// messages to the broker before the transaction commits; this worker takes
// them back off it and confirms delivery through the SAME MediatR command the
// HTTP confirm endpoint runs (MarkNotificationSent) - one contract whether
// the confirm comes from this worker, an external provider callback, or a
// human with Postman, so every pre-check and rule in that handler applies.
//
// Broker optional: with no RabbitMQ running the worker retries quietly and
// the API is unaffected - rows simply stay Queued. (Messages published into
// the void while the broker was down are the known publish-before-commit
// gap; an outbox is the real fix and is noted as future work.)
//
// at-least-once: a message is acked only after its confirm committed. A crash
// in between leaves it unacked, the broker redelivers, and duplicate confirms
// are a designed no-op - so redelivery is harmless.
internal sealed class RabbitNotificationConsumer(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitNotificationConsumer> logger) : BackgroundService
{
    // must match RabbitNotificationQueue - the two halves of one pipeline
    private const string Exchange = "commerce.notifications";
    private const string RoutingKey = "notification.dispatched";
    private const string Queue = "commerce.notifications.worker";
    private const string BindingKey = "notification.#"; // exact key today, per-channel keys later

    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ConnectionPollInterval = TimeSpan.FromSeconds(5);

    // Simulated provider latency - real email/SMS providers are not instant.
    // It also keeps the confirm clear of the publish-before-commit window, so
    // the row is committed by the time the worker looks for it.
    private readonly TimeSpan _deliveryDelay = TimeSpan.FromSeconds(
        int.TryParse(configuration["RabbitMq:DeliveryDelaySeconds"], out var seconds) && seconds >= 0
            ? seconds
            : 2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstFailure = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
                firstFailure = true; // connection ended cleanly - reset the log pattern
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // loud once, quiet afterwards: an API without a broker must not
                // drown in warnings every few seconds
                if (firstFailure)
                {
                    logger.LogWarning(
                        ex,
                        "Notification consumer cannot reach RabbitMQ; retrying every {Delay}s. Rows stay Queued until the broker is up.",
                        ReconnectDelay.TotalSeconds);
                    firstFailure = false;
                }
                else
                {
                    logger.LogDebug(ex, "Notification consumer retry after failure.");
                }

                try
                {
                    await Task.Delay(ReconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        logger.LogInformation("Notification consumer stopped.");
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        // own connection: publisher and consumer fail independently and neither
        // shares another's channel state (config keys are shared, by design)
        var port = int.TryParse(configuration["RabbitMq:Port"], out var parsed) ? parsed : 5672;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"] ?? "localhost",
            Port = port,
            UserName = configuration["RabbitMq:UserName"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(2), // fail fast when no broker
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // topology declare = idempotent; safe whether the publisher ran first
        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: BindingKey,
            cancellationToken: cancellationToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, args) =>
        {
            try
            {
                await HandleDeliveryAsync(channel, args, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // shutting down: deliberately no ack, so the broker redelivers
                // the in-flight message after the next start
            }
            catch (Exception ex)
            {
                // e.g. the channel died mid-ack: the message stays unacked and
                // is redelivered on reconnect; the poll below ends this session
                // and the outer loop reconnects
                logger.LogError(ex, "Delivery handling for tag {DeliveryTag} failed.", args.DeliveryTag);
            }
        };

        await channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Notification consumer connected: queue {Queue} bound to {Exchange} ({BindingKey}); delivery delay {Delay}s.",
            Queue, Exchange, BindingKey, _deliveryDelay.TotalSeconds);

        // stay subscribed until the connection drops or the host stops
        while (connection.IsOpen && !cancellationToken.IsCancellationRequested)
            await Task.Delay(ConnectionPollInterval, cancellationToken);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        CancellationToken cancellationToken)
    {
        if (!TryGetNotificationId(args, out var notificationId))
        {
            // poison message: it can never be confirmed - ack it away so it
            // does not loop forever, loudly enough to be noticed
            logger.LogError(
                "Dropping message {DeliveryTag}: no notification id in payload {Payload}.",
                args.DeliveryTag,
                ReadBody(args));
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            return;
        }

        if (_deliveryDelay > TimeSpan.Zero)
            await Task.Delay(_deliveryDelay, cancellationToken);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // the exact command POST /api/Notifications/{id}/sent runs
            await mediator.Send(new MarkNotificationSentCommand(notificationId), cancellationToken);

            logger.LogInformation(
                "Notification {NotificationId} delivered - confirmed Sent.", notificationId);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (NotFoundException ex)
        {
            // no row to confirm: the publish raced a commit that never landed.
            // Dropping is correct - requeueing would spin on a row that will
            // never exist (the outbox fixes the race itself).
            logger.LogWarning(
                ex,
                "Dropping message for {NotificationId}: no notification row (the commit never landed).",
                notificationId);
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (ConflictException ex)
        {
            // the row exists but is no longer confirmable (failed or read in
            // the meantime): the database wins, the message is satisfied
            logger.LogInformation(
                "Skipping confirm for {NotificationId}: {Reason}", notificationId, ex.Message);
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // no ack -> redelivery after shutdown (at-least-once)
        }
        catch (Exception ex)
        {
            // transient (database down?): leave unacked for redelivery - the
            // delivery delay paces the retries and duplicate confirms are no-ops
            logger.LogError(
                ex,
                "Confirm for {NotificationId} failed; the message will be redelivered.",
                notificationId);
            await channel.BasicNackAsync(
                args.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
        }
    }

    private static bool TryGetNotificationId(BasicDeliverEventArgs args, out Guid notificationId)
    {
        // the publisher stamps MessageId with the notification id
        var messageId = args.BasicProperties?.MessageId;
        if (!string.IsNullOrEmpty(messageId) && Guid.TryParse(messageId, out notificationId))
            return true;

        // fallback: the body is the serialized Notification (PascalCase - the
        // publisher uses plain JsonSerializer.Serialize, not the API's options)
        try
        {
            using var doc = JsonDocument.Parse(ReadBody(args));
            foreach (var name in new[] { "Id", "id" })
            {
                if (doc.RootElement.TryGetProperty(name, out var value)
                    && Guid.TryParse(value.GetString(), out notificationId))
                    return true;
            }
        }
        catch (JsonException)
        {
            // unreadable body - handled as a poison message by the caller
        }

        notificationId = Guid.Empty;
        return false;
    }

    private static string ReadBody(BasicDeliverEventArgs args)
        => Encoding.UTF8.GetString(args.Body.Span);
}
