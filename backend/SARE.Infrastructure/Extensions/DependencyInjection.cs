using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SARE.Application.Common.Interfaces;
using SARE.Infrastructure.Persistence;
using SARE.Infrastructure.Persistence.Repositories;
using SARE.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace SARE.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductOptionRepository, ProductOptionRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddSingleton<IProductImageStore>(provider => new LocalProductImageStore(
            configuration["ProductImages:StoragePath"] ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "product-images"),
            provider.GetRequiredService<ILogger<LocalProductImageStore>>()));

        return services;
    }
}
