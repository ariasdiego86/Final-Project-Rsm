using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Tests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=test;Database=test",
                ["GoogleMaps:ApiKey"] = "test-key",
                ["GoogleMaps__ApiKey"] = "test-key",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
            }));

        builder.ConfigureServices(services =>
        {
            // EF Core 8+ registers IDbContextOptionsConfiguration<T> alongside DbContextOptions<T>.
            // Both must be removed or two providers coexist and throw.
            var efDescriptors = services
                .Where(d => d.ServiceType.IsGenericType
                         && d.ServiceType.GenericTypeArguments.Length > 0
                         && d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))
                .ToList();
            foreach (var d in efDescriptors) services.Remove(d);

            var dbName = $"IntegrationTestDb_{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(dbName));

            var geoDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IGeoLocationService));
            if (geoDescriptor is not null) services.Remove(geoDescriptor);

            services.AddScoped<IGeoLocationService, StubGeoLocationService>();
        });

        builder.UseEnvironment("Testing");
    }

    public void SeedDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        SeedData.Seed(db);
    }
}

internal class StubGeoLocationService : IGeoLocationService
{
    public Task<AddressValidationResultDto> ValidateAddressAsync(string address, CancellationToken ct = default) =>
        Task.FromResult(new AddressValidationResultDto
        {
            FormattedAddress = address,
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            IsValid = true,
            Issues = []
        });
}
