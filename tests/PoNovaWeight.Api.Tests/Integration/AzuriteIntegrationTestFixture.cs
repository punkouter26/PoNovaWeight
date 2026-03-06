using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Azurite;
using Xunit;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Api.Infrastructure.OpenAI;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Web Application Factory with real Azurite container for full integration tests.
/// Provides actual table storage (via Testcontainers) instead of mocks.
/// Use CustomWebApplicationFactory for fast unit-style tests with mocks.
/// Use this for comprehensive endpoint testing with real storage.
/// </summary>
public class AzuriteIntegrationTestFixture : WebApplicationFactory<Program>
{
    private AzuriteContainer? _azuriteContainer;

    public async Task InitializeAsync()
    {
        _azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithCleanUp(true)
            .Build();

        await _azuriteContainer.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_azuriteContainer != null)
        {
            await _azuriteContainer.StopAsync();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            if (_azuriteContainer == null)
            {
                throw new InvalidOperationException("Azurite container must be initialized before tests run. Use async fixture initialization.");
            }

            // Remove default TableServiceClient
            services.RemoveAll<TableServiceClient>();

            // Add real table storage client pointing to Azurite container
            var connectionString = _azuriteContainer.GetConnectionString();
            services.AddSingleton(new TableServiceClient(connectionString));

            // Add session services required by Program.cs middleware
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Register real repositories (will use Azurite table storage)
            services.RemoveAll<IDailyLogRepository>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IUserSettingsRepository>();

            services.AddSingleton<IDailyLogRepository, DailyLogRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IUserSettingsRepository, UserSettingsRepository>();

            // Still use stubs for AI services to avoid real OpenAI calls
            services.RemoveAll<IMealAnalysisService>();
            services.AddSingleton<IMealAnalysisService, StubMealAnalysisService>();
        });

        // Configure test settings
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "test-client-id",
                ["Google:ClientSecret"] = "test-client-secret",
            });
        });
    }
}

/// <summary>
/// Collection definition for Azurite-based integration tests with real table storage.
/// Usage: Add [Collection("Azurite Integration Tests Collection")] to test classes.
/// Example:
/// [Collection("Azurite Integration Tests Collection")]
/// public class MyIntegrationTests : IAsyncLifetime
/// {
///     private readonly AzuriteIntegrationTestFixture _factory;
///     private HttpClient _client;
///
///     public MyIntegrationTests()
///     {
///         _factory = new AzuriteIntegrationTestFixture();
///     }
///
///     public async Task InitializeAsync()
///     {
///         await _factory.InitializeAsync();
///         _client = _factory.CreateClient();
///     }
///
///     public async Task DisposeAsync()
///     {
///         await _factory.DisposeAsync();
///         _client.Dispose();
///     }
/// }
/// </summary>
[CollectionDefinition("Azurite Integration Tests Collection")]
public class AzuriteIntegrationTestCollection
{
}
