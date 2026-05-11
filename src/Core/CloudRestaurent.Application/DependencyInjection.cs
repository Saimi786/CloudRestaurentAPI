using System.Reflection;
using CloudRestaurent.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        // Always scan Core/Application + any module assemblies the caller passes in.
        // Without this, MediatR can't see handlers that live alongside their domain
        // (e.g. LoginCommand in Modules.Identity) — they'd silently fail to resolve at
        // request time. Module installers expose `RegisterServices` for DI but don't
        // own handler discovery; that lives here so behaviors apply uniformly.
        var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
        assemblies.AddRange(additionalAssemblies.Distinct());

        services.AddMediatR(cfg =>
        {
            foreach (var a in assemblies)
                cfg.RegisterServicesFromAssembly(a);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        foreach (var a in assemblies)
            services.AddValidatorsFromAssembly(a);

        return services;
    }
}
