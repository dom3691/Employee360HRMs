using System.Reflection;
using Employee360.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Employee360.Application;

/// <summary>
/// Registers all Application-layer services (MediatR handlers, FluentValidation
/// validators, AutoMapper profiles, and pipeline behaviours) into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application-layer services. Called once from the API composition root.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}
