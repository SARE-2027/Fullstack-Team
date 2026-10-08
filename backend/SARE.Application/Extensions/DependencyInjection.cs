using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SARE.Application.Common.Interfaces;
using SARE.Application.Services;

namespace SARE.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ISessionService, SessionService>();
        return services;
    }
}

