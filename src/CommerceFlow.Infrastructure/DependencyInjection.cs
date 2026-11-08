using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Infrastructure.Catalog.Brands;
using CommerceFlow.Infrastructure.Catalog.Categories;
using CommerceFlow.Infrastructure.Catalog.ProductAttributes;
using CommerceFlow.Infrastructure.Catalog.Products;
using CommerceFlow.Infrastructure.Persistence;
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
        return services;
    }
}
