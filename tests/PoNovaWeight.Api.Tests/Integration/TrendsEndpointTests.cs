using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for the weight trends endpoint.
/// </summary>
[Collection("Integration Tests")]
public class TrendsEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TrendsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTrends_ReturnsOk_WithTrendData()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.ToString("yyyy-MM-dd"),
                Weight = 180.5
            },
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.AddDays(-5).ToString("yyyy-MM-dd"),
                Weight = 182.0
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
        var response = await client.GetAsync("/api/daily-logs/trends?days=7");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WeightTrendsDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalDaysLogged);
        Assert.NotNull(result.WeightChange);
    }

    [Fact]
    public async Task GetTrends_ReturnsEmpty_WhenNoData()
    {
        // Arrange
        var mockRepo = new Mock<IDailyLogRepository>();
        mockRepo.Setup(r => r.GetRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyLogEntity>());

        var client = CreateClientWithMockedRepository(mockRepo);

        // Act
        var response = await client.GetAsync("/api/daily-logs/trends?days=30");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WeightTrendsDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalDaysLogged);
        Assert.Null(result.WeightChange);
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
