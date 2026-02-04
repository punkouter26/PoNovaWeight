using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for the monthly logs endpoint.
/// </summary>
[Collection("Integration Tests")]
public class MonthlyLogsEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public MonthlyLogsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMonthlyLogs_ReturnsOk_WithMonthData()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entities = new List<DailyLogEntity>
        {
            new()
            {
                PartitionKey = "dev-user",
                RowKey = today.ToString("yyyy-MM-dd"),
                OmadCompliant = true,
                Weight = 175.5
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
        var response = await client.GetAsync($"/api/daily-logs/monthly/{today.Year}/{today.Month}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MonthlyLogsDto>();
        Assert.NotNull(result);
        Assert.Equal(today.Year, result.Year);
        Assert.Equal(today.Month, result.Month);
        Assert.Single(result.Days);
    }

    [Fact]
    public async Task GetMonthlyLogs_ReturnsEmpty_WhenNoData()
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
        var response = await client.GetAsync("/api/daily-logs/monthly/2024/6");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MonthlyLogsDto>();
        Assert.NotNull(result);
        Assert.Empty(result.Days);
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
