using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for API endpoints using WebApplicationFactory.
/// These tests use mocked repositories to avoid Azurite dependency.
/// </summary>
[Collection("Integration Tests")]
public class EndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public EndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Arrange
        var client = CreateClientWithMockedDependencies();

        // Act - uses Aspire default /health endpoint
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content.ToLowerInvariant());
    }

    [Fact]
    public async Task GetDailyLog_ExistingEntry_ReturnsOk()
    {
        // Arrange
        var testDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var mockEntity = new DailyLogEntity
        {
            PartitionKey = "dev-user",
            RowKey = testDate.ToString("yyyy-MM-dd"),
            Proteins = 3,
            Vegetables = 4,
            Fruits = 2,
            Starches = 2,
            Fats = 2,
            Dairy = 1,
            WaterSegments = 6
        };

        var client = CreateClientWithMockedDependencies(mockEntity, null, authenticated: true);

        // Act
        var response = await client.GetAsync($"/api/daily-logs/{testDate:yyyy-MM-dd}");

        // Assert
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DailyLogDto>();
        Assert.NotNull(dto);
        Assert.Equal(testDate, dto.Date);
        Assert.Equal(3, dto.Proteins);
        Assert.Equal(4, dto.Vegetables);
    }

    [Fact]
    public async Task GetDailyLog_NonExistentEntry_ReturnsEmptyLog()
    {
        // Arrange
        var testDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var client = CreateClientWithMockedDependencies(null, null, authenticated: true);

        // Act
        var response = await client.GetAsync($"/api/daily-logs/{testDate:yyyy-MM-dd}");

        // Assert - API returns 200 with empty log (not 404) to avoid client console errors
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var log = await response.Content.ReadFromJsonAsync<DailyLogDto>();
        Assert.NotNull(log);
        Assert.Equal(testDate, log.Date);
        Assert.Equal(0, log.Proteins);
    }

    [Fact]
    public async Task GetDailyLog_InvalidDate_ReturnsError()
    {
        // Arrange
        var client = CreateClientWithMockedDependencies(authenticated: true);

        // Act
        var response = await client.GetAsync("/api/daily-logs/not-a-date");

        // Assert - Invalid date binding results in error (400 or 500 depending on exception handling)
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected BadRequest or InternalServerError, got {response.StatusCode}");
    }

    [Fact]
    public async Task SaveDailyLog_ValidData_ReturnsOk()
    {
        // Arrange
        var testDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = new DailyLogDto
        {
            Date = testDate,
            Proteins = 3,
            Vegetables = 4,
            Fruits = 2,
            Starches = 2,
            Fats = 2,
            Dairy = 1,
            WaterSegments = 6
        };

        var mockRepo = new Mock<IDailyLogRepository>();
        mockRepo.Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockRepo.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyLogEntity?)null);

        var client = CreateClientWithMockedDependencies(mockRepo: mockRepo, authenticated: true);

        // Act - API uses PUT /api/daily-logs/ (date in body, not URL)
        var response = await client.PutAsJsonAsync("/api/daily-logs/", dto);

        // Assert
        response.EnsureSuccessStatusCode();
        mockRepo.Verify(r => r.UpsertAsync(
            It.Is<DailyLogEntity>(e => e.Proteins == 3 && e.Vegetables == 4),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWeeklySummary_ReturnsCorrectDateRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var entities = new List<DailyLogEntity>
        {
            new() { PartitionKey = "dev-user", RowKey = today.ToString("yyyy-MM-dd"), Proteins = 3 },
            new() { PartitionKey = "dev-user", RowKey = today.AddDays(-1).ToString("yyyy-MM-dd"), Proteins = 2 }
        };

        var mockRepo = new Mock<IDailyLogRepository>();
        mockRepo.Setup(r => r.GetRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var client = CreateClientWithMockedDependencies(mockRepo: mockRepo, authenticated: true);

        // Act - API requires date parameter: GET /api/weekly-summary/{date}
        var response = await client.GetAsync($"/api/weekly-summary/{today:yyyy-MM-dd}");

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<WeeklySummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(7, summary.Days.Count);
    }

    [Fact]
    public async Task AuthMe_NoSession_ReturnsUnauthenticated()
    {
        // Arrange
        var client = CreateClientWithMockedDependencies();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthStatus>();
        Assert.NotNull(result);
        Assert.False(result.IsAuthenticated);
        Assert.Null(result.User);
    }

    // NOTE: AuthLogin_RedirectsToGoogle test moved to AuthEndpointsTests.cs
    // to avoid Serilog bootstrap logger conflicts with other tests in this class.
    // See AuthEndpointsTests.GetLogin_ReturnsRedirectToGoogle for equivalent coverage.

    private HttpClient CreateClientWithMockedDependencies(
        DailyLogEntity? returnEntity = null,
        Mock<IDailyLogRepository>? mockRepo = null,
        bool authenticated = false)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real repository registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDailyLogRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add mocked repository
                if (mockRepo == null)
                {
                    mockRepo = new Mock<IDailyLogRepository>();
                    mockRepo.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(returnEntity);
                    mockRepo.Setup(r => r.GetRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(returnEntity != null ? new List<DailyLogEntity> { returnEntity } : new List<DailyLogEntity>());
                    mockRepo.Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
                }

                services.AddSingleton(mockRepo.Object);

                // Add test authentication only when explicitly requested via the 'authenticated' parameter
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

/// <summary>
/// Response DTO for auth status endpoint.
/// </summary>
public record AuthStatusResponse
{
    public bool Authenticated { get; init; }
}
