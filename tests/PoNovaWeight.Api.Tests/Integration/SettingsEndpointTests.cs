using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Integration tests for Settings endpoints (/api/settings).
/// Tests authenticated access, defaults, and upsert operations.
/// </summary>
[Collection("Integration Tests")]
public class SettingsEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SettingsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSettings_Authenticated_ReturnsDefaults_WhenNoSettingsExist()
    {
        // Arrange
        var mockRepo = new Mock<IUserSettingsRepository>();
        mockRepo
            .Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSettingsEntity?)null);

        var client = CreateClientWithMockedRepository(mockRepo, authenticated: true);

        // Act
        var response = await client.GetAsync("/api/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        settings.Should().NotBeNull();
        settings!.TargetSystolic.Should().Be(120);
        settings.TargetDiastolic.Should().Be(80);
        settings.TargetHeartRate.Should().Be(70);
    }

    [Fact]
    public async Task GetSettings_Authenticated_ReturnsCustomSettings()
    {
        // Arrange
        var mockEntity = UserSettingsEntity.Create("dev-user@local");
        mockEntity.TargetSystolic = 115;
        mockEntity.TargetDiastolic = 75;
        mockEntity.TargetHeartRate = 65;

        var mockRepo = new Mock<IUserSettingsRepository>();
        mockRepo
            .Setup(r => r.GetAsync("dev-user@local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEntity);

        var client = CreateClientWithMockedRepository(mockRepo, authenticated: true);

        // Act
        var response = await client.GetAsync("/api/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        settings.Should().NotBeNull();
        settings!.TargetSystolic.Should().Be(115);
        settings.TargetDiastolic.Should().Be(75);
        settings.TargetHeartRate.Should().Be(65);
    }

    [Fact]
    public async Task GetSettings_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var mockRepo = new Mock<IUserSettingsRepository>();
        var client = CreateClientWithMockedRepository(mockRepo, authenticated: false);

        // Act
        var response = await client.GetAsync("/api/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutSettings_Authenticated_UpsertsSettings()
    {
        // Arrange
        var newSettings = new UserSettingsDto
        {
            TargetSystolic = 125,
            TargetDiastolic = 82,
            TargetHeartRate = 68
        };

        var mockRepo = new Mock<IUserSettingsRepository>();
        mockRepo
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClientWithMockedRepository(mockRepo, authenticated: true);

        // Act
        var response = await client.PutAsJsonAsync("/api/settings", newSettings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedSettings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        returnedSettings.Should().NotBeNull();
        returnedSettings!.TargetSystolic.Should().Be(125);
        returnedSettings.TargetDiastolic.Should().Be(82);
        returnedSettings.TargetHeartRate.Should().Be(68);

        mockRepo.Verify(
            r => r.UpsertAsync(
                It.Is<UserSettingsEntity>(e =>
                    e.RowKey == "dev-user@local" &&
                    e.PartitionKey == "settings" &&
                    e.TargetSystolic == 125 &&
                    e.TargetDiastolic == 82 &&
                    e.TargetHeartRate == 68),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PutSettings_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var settings = new UserSettingsDto
        {
            TargetSystolic = 120,
            TargetDiastolic = 80,
            TargetHeartRate = 70
        };

        var mockRepo = new Mock<IUserSettingsRepository>();
        var client = CreateClientWithMockedRepository(mockRepo, authenticated: false);

        // Act
        var response = await client.PutAsJsonAsync("/api/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        mockRepo.Verify(
            r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PutSettings_PartialSettings_UpsertsSuccessfully()
    {
        // Arrange
        var partialSettings = new UserSettingsDto
        {
            TargetSystolic = 130,
            TargetDiastolic = null,
            TargetHeartRate = 72
        };

        var mockRepo = new Mock<IUserSettingsRepository>();
        mockRepo
            .Setup(r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClientWithMockedRepository(mockRepo, authenticated: true);

        // Act
        var response = await client.PutAsJsonAsync("/api/settings", partialSettings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedSettings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        returnedSettings.Should().NotBeNull();
        returnedSettings!.TargetSystolic.Should().Be(130);
        returnedSettings.TargetDiastolic.Should().BeNull();
        returnedSettings.TargetHeartRate.Should().Be(72);
    }

    [Fact]
    public async Task PutSettings_InvalidPayload_ReturnsInternalServerError()
    {
        // Arrange - ASP.NET Core minimal APIs return 500 for JSON deserialization failures
        var mockRepo = new Mock<IUserSettingsRepository>();
        var client = CreateClientWithMockedRepository(mockRepo, authenticated: true);

        // Act - Send malformed JSON
        var content = new StringContent("{invalid json", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/settings", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        mockRepo.Verify(
            r => r.UpsertAsync(It.IsAny<UserSettingsEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private HttpClient CreateClientWithMockedRepository(Mock<IUserSettingsRepository> mockRepo, bool authenticated = true)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace IUserSettingsRepository with mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUserSettingsRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(mockRepo.Object);

                if (authenticated)
                {
                    // Add TestAuthHandler for authenticated requests
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, 
                        TestInfrastructure.TestAuthHandler>("Test", _ => { });

                    services.AddAuthorization(options =>
                    {
                        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Test")
                            .RequireAuthenticatedUser()
                            .Build();
                    });
                }
            });
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
