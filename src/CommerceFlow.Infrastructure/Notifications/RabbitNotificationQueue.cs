using System.Text;
using System.Text.Json;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Domain.Entities.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace CommerceFlow.Infrastructure.Notifications;

// RabbitMQ implementation of the queue port - the one class that knows the
// broker's name, so the Application layer and the Domain stay transport-free.
//
// Everything is lazy: the connection opens on the first publish, never at
// boot, so a missing broker cannot stop the API from starting (the DI crash
// this module had was about registration, not connectivity).
//
// Best-effort publish: when the broker is unreachable the failure is logged
// loudly and the send still completes - the database row is the source of
// truth and stays Queued, which is exactly where a notification waiting for
// its delivery worker belongs. A production deployment would fail the request
// (or dead-letter) instead; the choice is logged so it cannot pass unnoticed.
internal sealed class RabbitNotificationQueue : INotificationQueue, IDisposable
{
    private const string Exchange = "commerce.notifications";
    private const string RoutingKey = "notification.dispatched";

    private readonly ILogger<RabbitNotificationQueue> _logger;
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitNotificationQueue(
        IConfiguration configuration,
        ILogger<RabbitNotificationQueue> logger)
    {
        _logger = logger;

        var port = int.TryParse(configuration["RabbitMq:Port"], out var parsed)
            ? parsed
            : 5672;

        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"] ?? "localhost",
            Port = port,
            UserName = configuration["RabbitMq:UserName"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(2), // fail fast when no broker
        };
    }

    public async Task PublishAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));

            var properties = new BasicProperties
            {
                Persistent = true, // survive a broker restart
                MessageId = notification.Id.ToString(),
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            };

            await channel.BasicPublishAsync(
                exchange: Exchange,
                routingKey: RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Published notification {NotificationId} ({Channel}) to {Exchange} / {RoutingKey}.",
                notification.Id, notification.Channel, Exchange, RoutingKey);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // best-effort by design (see class comment): the row stays the
            // source of truth; the delivery worker or a retry carries it later
            _logger.LogWarning(
                ex,
                "RabbitMQ unavailable - notification {NotificationId} was NOT published. The row stays queued; start the broker to drain it.",
                notification.Id);
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not { IsOpen: true })
                _connection = await _factory.CreateConnectionAsync(cancellationToken);

            if (_channel is not { IsOpen: true })
            {
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // topic exchange so routing can grow (per-channel keys)
                // without touching the publisher
                await _channel.ExchangeDeclareAsync(
                    exchange: Exchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    cancellationToken: cancellationToken);
            }

            return _channel;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _connectLock.Dispose();
    }
}
