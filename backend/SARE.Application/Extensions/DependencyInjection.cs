using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SARE.Application.Services;
using SARE.Application.Common.Interfaces;

namespace SARE.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<ProductService>();
        services.AddScoped<ProductOptionService>();
        services.AddScoped<ProductVariantService>();
        services.AddScoped<ProductImageService>();
        services.AddScoped<CatalogDashboardService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ICartService, CartService>();
        services.AddValidatorsFromAssemblyContaining<CategoryService>();
        return services;
    }
}
