using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for the alcohol correlation endpoint.
/// </summary>
[Collection("Integration Tests")]
public class AlcoholCorrelationEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AlcoholCorrelationEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAlcoholCorrelation_ReturnsOk_WithCorrelationData()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.ToString("yyyy-MM-dd"),
                Weight = 180.0,
                AlcoholConsumed = true
            },
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.AddDays(-1).ToString("yyyy-MM-dd"),
                Weight = 178.0,
                AlcoholConsumed = false
            }
        };

        var mockRepo = new Mock<IDailyLogRepository>();
        mockRepo.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var client = CreateClientWithMockedRepository(mockRepo);

        // Act
        var response = await client.GetAsync("/api/daily-logs/alcohol-correlation?days=30");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AlcoholCorrelationDto>();
        Assert.NotNull(result);
        Assert.True(result.HasSufficientData);
        Assert.Equal(1, result.DaysWithAlcohol);
        Assert.Equal(1, result.DaysWithoutAlcohol);
    }

    [Fact]
    public async Task GetAlcoholCorrelation_ReturnsInsufficientData_WhenNoAlcoholDays()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.ToString("yyyy-MM-dd"),
                Weight = 180.0,
                AlcoholConsumed = false
            }
        };

        var mockRepo = new Mock<IDailyLogRepository>();
        mockRepo.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var client = CreateClientWithMockedRepository(mockRepo);

        // Act
        var response = await client.GetAsync("/api/daily-logs/alcohol-correlation?days=30");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AlcoholCorrelationDto>();
        Assert.NotNull(result);
        Assert.False(result.HasSufficientData);
    }

    private HttpClient CreateClientWithMockedRepository(Mock<IDailyLogRepository> mockRepo, bool authenticated = true)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDailyLogRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockRepo.Object);

                if (authenticated)
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, PoNovaWeight.Api.Tests.Integration.TestInfrastructure.TestAuthHandler>(
                        "Test", _ => { });

                    services.AddAuthorization(options =>
                    {
                        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Test")
                            .RequireAuthenticatedUser()
                            .Build();
                    });
                }
            });
        });

        return factory.CreateClient();
    }
}
