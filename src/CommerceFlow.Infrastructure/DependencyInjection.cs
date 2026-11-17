using CommerceFlow.Application.Addresses.Interfaces;
using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Inventory.Interfaces;
using CommerceFlow.Application.Notifications.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Payments.Gateways;
using CommerceFlow.Application.Payments.Interfaces;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Application.Shipping.Interfaces;
using CommerceFlow.Application.Wishlists.Interfaces;
using CommerceFlow.Infrastructure.Addresses;
using CommerceFlow.Infrastructure.Auth;
using CommerceFlow.Infrastructure.Catalog.Brands;
using CommerceFlow.Infrastructure.Carts;
using CommerceFlow.Infrastructure.Catalog.Categories;
using CommerceFlow.Infrastructure.Catalog.ProductAttributes;
using CommerceFlow.Infrastructure.Catalog.Products;
using CommerceFlow.Infrastructure.Inventory.InventoryItems;
using CommerceFlow.Infrastructure.Inventory.Reservations;
using CommerceFlow.Infrastructure.Inventory.StockMovements;
using CommerceFlow.Infrastructure.Inventory.Warehouses;
using CommerceFlow.Infrastructure.Notifications;
using CommerceFlow.Infrastructure.Orders;
using CommerceFlow.Infrastructure.Payments;
using CommerceFlow.Infrastructure.Payments.Gateways;
using CommerceFlow.Infrastructure.Persistence;
using CommerceFlow.Infrastructure.Promotions;
using CommerceFlow.Infrastructure.Returns;
using CommerceFlow.Infrastructure.Reviews;
using CommerceFlow.Infrastructure.Shipping;
using CommerceFlow.Infrastructure.Wishlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<CommerceFlowDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();

        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IInventoryReservationRepository, InventoryReservationRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddScoped<IShipmentRepository, ShipmentRepository>();

        services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();

        services.AddScoped<IPromotionRepository, PromotionRepository>();

        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Auth: the repository is per-request; the hasher and the token
        // service are stateless (a key + a KDF), so they live as singletons
        // and are shared by every login.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // The transport lives behind the port: RabbitMQ today, an in-process
        // channel could replace it with one line - Application never sees the
        // broker. Singleton because it owns the (lazy) connection.
        services.AddSingleton<INotificationQueue, RabbitNotificationQueue>();

        // ...and the other half of that transport: the in-process worker that
        // drains the queue and confirms delivery through the same command the
        // /sent endpoint runs. Hosted services are singletons by nature; a
        // missing broker only costs it a quiet reconnect loop.
        services.AddHostedService<RabbitNotificationConsumer>();

        // Strategy: one class per provider + one line here to add another
        // (open/closed). The resolver picks the implementation by name.
        services.AddScoped<IPaymentGateway, FakePayGateway>();
        services.AddScoped<IPaymentGateway, StripeGateway>();
        services.AddScoped<IPaymentGateway, PayPalGateway>();
        services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();
        return services;
    }
}
