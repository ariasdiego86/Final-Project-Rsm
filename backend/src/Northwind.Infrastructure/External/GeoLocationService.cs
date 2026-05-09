using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Northwind.Application.DTOs;
using Northwind.Application.Interfaces;
using Northwind.Domain.Exceptions;

namespace Northwind.Infrastructure.External;

public class GeoLocationService : IGeoLocationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public GeoLocationService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["GoogleMaps__ApiKey"]
            ?? configuration["GoogleMaps:ApiKey"]
            ?? throw new InvalidOperationException("Google Maps API key is not configured.");
    }

    public async Task<AddressValidationResultDto> ValidateAddressAsync(string address, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("GoogleMaps");
        var requestBody = JsonSerializer.Serialize(new
        {
            address = new { addressLines = new[] { address } }
        });

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                $"./v1:validateAddress?key={_apiKey}",
                new StringContent(requestBody, Encoding.UTF8, "application/json"),
                ct);
        }
        catch (TaskCanceledException)
        {
            throw new DomainException("Address validation request timed out. Please try again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if ((int)response.StatusCode == 400 && errorBody.Contains("Unsupported region code"))
                return await GeocodeAddressAsync(address, ct);

            throw new DomainException($"Address validation failed with status {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("result");

        var formattedAddress = result
            .GetProperty("address")
            .GetProperty("formattedAddress")
            .GetString() ?? address;

        decimal? latitude = null;
        decimal? longitude = null;
        if (result.TryGetProperty("geocode", out var geocode) &&
            geocode.TryGetProperty("location", out var location))
        {
            if (location.TryGetProperty("latitude", out var lat))
                latitude = (decimal)lat.GetDouble();
            if (location.TryGetProperty("longitude", out var lng))
                longitude = (decimal)lng.GetDouble();
        }

        var issues = new List<string>();
        if (result.TryGetProperty("verdict", out var verdict))
        {
            if (verdict.TryGetProperty("hasInferredComponents", out var inferred) && inferred.GetBoolean())
                issues.Add("Address has inferred components.");
            if (verdict.TryGetProperty("hasReplacedComponents", out var replaced) && replaced.GetBoolean())
                issues.Add("Some address components were replaced.");
        }

        return new AddressValidationResultDto
        {
            FormattedAddress = formattedAddress,
            Latitude = latitude,
            Longitude = longitude,
            IsValid = latitude.HasValue && longitude.HasValue,
            Issues = issues.ToArray()
        };
    }

    private async Task<AddressValidationResultDto> GeocodeAddressAsync(string address, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("GoogleGeocoding");
        var encodedAddress = Uri.EscapeDataString(address);
        var response = await client.GetAsync($"maps/api/geocode/json?address={encodedAddress}&key={_apiKey}", ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("status").GetString();

        if (status != "OK")
            throw new DomainException($"Geocoding failed: {status}");

        var locationEl = doc.RootElement
            .GetProperty("results")[0]
            .GetProperty("geometry")
            .GetProperty("location");

        var formattedAddress = doc.RootElement
            .GetProperty("results")[0]
            .GetProperty("formatted_address")
            .GetString() ?? address;

        var latitude = (decimal)locationEl.GetProperty("lat").GetDouble();
        var longitude = (decimal)locationEl.GetProperty("lng").GetDouble();

        return new AddressValidationResultDto
        {
            FormattedAddress = formattedAddress,
            Latitude = latitude,
            Longitude = longitude,
            IsValid = true,
            Issues = Array.Empty<string>()
        };
    }
}
