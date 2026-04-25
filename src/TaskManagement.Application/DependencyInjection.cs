using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Common.Behaviors;
using TaskManagement.Application.Tasks.Ordering;

namespace TaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Namespace = "TaskManagement.Application.Mediator";
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IOrderKeyService, OrderKeyService>();

        // Singleton: gate cache must outlive a request or the lock is meaningless.
        services.AddSingleton<IReorderSerializer, PerOwnerReorderSerializer>();

        return services;
    }
}
