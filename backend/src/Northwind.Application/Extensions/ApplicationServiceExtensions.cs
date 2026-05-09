using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Northwind.Application.Interfaces;
using Northwind.Application.Mappings;
using Northwind.Application.Services;
using Northwind.Application.Validators;
using System.Reflection;

namespace Northwind.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(OrderProfile).Assembly));
        services.AddValidatorsFromAssembly(typeof(CreateOrderDtoValidator).Assembly);
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ILookupService, LookupService>();
        return services;
    }
}
