using EventsApi.Data.Repositories;
using EventsApi.Mappers;
using EventsApi.Services;
using EventsApi.Validation;

namespace EventsApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationService(this IServiceCollection services)
    {
        services.AddSingleton<EventMapper>();
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();
        services.AddScoped<IEventService, EventService>();
        return services;
    }

    public static IServiceCollection AddModelValidation(this IServiceCollection services)
    {
        services.AddScoped<IModelValidator, DataAnnotationsModelValidator>();
        return services;
    }
}
