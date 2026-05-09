using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Northwind.Application.Interfaces;
using Northwind.Infrastructure.External;
using Northwind.Infrastructure.Persistence;
using Northwind.Infrastructure.Persistence.Repositories;
using Northwind.Infrastructure.Reports;

namespace Northwind.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings__Default"]
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHttpClient("GoogleMaps", client =>
        {
            client.BaseAddress = new Uri("https://addressvalidation.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient("GoogleGeocoding", client =>
        {
            client.BaseAddress = new Uri("https://maps.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IGeoLocationService, GeoLocationService>();
        services.AddScoped<IPdfReportService, PdfReportService>();
        services.AddScoped<IExcelReportService, ExcelReportService>();

        return services;
    }
}
