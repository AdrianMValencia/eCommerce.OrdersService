using eCommerce.OrdersService.Api.Abstractions.Messaging;
using eCommerce.OrdersService.Api.Features.Products.Consumers;
using eCommerce.OrdersService.Api.Features.Products.HostedServices;
using eCommerce.OrdersService.Api.Shared.Behaviors;
using FluentValidation;
using Mapster;
using MongoDB.Driver;
using System.Reflection;

namespace eCommerce.OrdersService.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        string connectionStringTemplate = configuration.GetConnectionString("OrdersServiceConnection")!;

        string connectionString = connectionStringTemplate
            .Replace("$MONGODB_HOST", Environment.GetEnvironmentVariable("MONGODB_HOST"))
            .Replace("$MONGODB_PORT", Environment.GetEnvironmentVariable("MONGODB_PORT"));

        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));

        services.AddScoped<IMongoDatabase>(providers =>
        {
            IMongoClient mongoClient = providers.GetRequiredService<IMongoClient>();
            return mongoClient.GetDatabase(Environment.GetEnvironmentVariable("MONGODB_DATABASE"));
        });

        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<HandlerExecutor>();
        services.AddScoped<IValidationService, ValidationService>();

        services.AddHandlersFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IDispatcher, Dispatcher>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}";
        });

        services.AddTransient<IProductNameUpdatedConsumer, ProductNameUpdatedConsumer>();
        services.AddHostedService<ProductNameUpdateHostedService>();

        return services;
    }

    private static void AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))));

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

            foreach (var handlerInterface in interfaces)
            {
                // Registra cada handler con su interfaz correspondiente
                services.AddScoped(handlerInterface, handlerType);
            }
        }
    }
}
