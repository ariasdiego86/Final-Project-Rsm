using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Northwind.Domain.Exceptions;
using Northwind.Infrastructure.External;

namespace Northwind.Tests.Infrastructure.External;

public class GeoLocationServiceTests
{
    private const string ValidResponse = """
        {
          "result": {
            "address": { "formattedAddress": "Obere Str. 57, Berlin, Germany" },
            "geocode": { "location": { "latitude": 52.52, "longitude": 13.40 } },
            "verdict": { "hasInferredComponents": false, "hasReplacedComponents": false }
          }
        }
        """;

    private static GeoLocationService BuildService(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("GoogleMaps"))
            .Returns(new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("https://addressvalidation.googleapis.com/"),
                Timeout = TimeSpan.FromSeconds(5)
            });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["GoogleMaps__ApiKey"] = "test-key" })
            .Build();

        return new GeoLocationService(factory.Object, config);
    }

    [Fact]
    public async Task ValidateAddressAsync_returns_coordinates_on_success()
    {
        var sut = BuildService(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(ValidResponse, Encoding.UTF8, "application/json")
        });

        var result = await sut.ValidateAddressAsync("Obere Str. 57, Berlin, Germany");

        result.IsValid.Should().BeTrue();
        result.FormattedAddress.Should().Be("Obere Str. 57, Berlin, Germany");
        result.Latitude.Should().Be(52.52m);
        result.Longitude.Should().Be(13.40m);
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAddressAsync_returns_issues_when_inferred_components()
    {
        const string responseWithIssues = """
            {
              "result": {
                "address": { "formattedAddress": "Somewhere" },
                "geocode": { "location": { "latitude": 10.0, "longitude": 20.0 } },
                "verdict": { "hasInferredComponents": true, "hasReplacedComponents": false }
              }
            }
            """;

        var sut = BuildService(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseWithIssues, Encoding.UTF8, "application/json")
        });

        var result = await sut.ValidateAddressAsync("Somewhere");

        result.IsValid.Should().BeTrue();
        result.Issues.Should().ContainSingle(i => i.Contains("inferred"));
    }

    [Fact]
    public async Task ValidateAddressAsync_throws_DomainException_on_4xx_response()
    {
        var sut = BuildService(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        Func<Task> act = () => sut.ValidateAddressAsync("Bad address");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*400*");
    }

    [Fact]
    public async Task ValidateAddressAsync_throws_DomainException_on_unauthorized()
    {
        var sut = BuildService(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        Func<Task> act = () => sut.ValidateAddressAsync("Some address");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*401*");
    }
}
