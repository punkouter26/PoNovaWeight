using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for the streak endpoint.
/// </summary>
[Collection("Integration Tests")]
public class StreakEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public StreakEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStreak_ReturnsOk_WithStreakData()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.ToString("yyyy-MM-dd"),
                OmadCompliant = true
            },
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.AddDays(-1).ToString("yyyy-MM-dd"),
                OmadCompliant = true
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
        var response = await client.GetAsync("/api/daily-logs/streak");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StreakDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.CurrentStreak);
    }

    [Fact]
    public async Task GetStreak_ReturnsZero_WhenNoCompliantDays()
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
        var response = await client.GetAsync("/api/daily-logs/streak");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StreakDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result.CurrentStreak);
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
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, PoNovaWeight.Api.Tests.TestAuth.TestAuthHandler>(
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
