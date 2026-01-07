using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration tests that properly configures
/// the test host without Aspire dependencies.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove Aspire-configured TableServiceClient if present
            services.RemoveAll<TableServiceClient>();

            // Add test TableServiceClient with development storage
            services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));

            // Remove existing repository registrations
            services.RemoveAll<IDailyLogRepository>();
            services.RemoveAll<IUserRepository>();

            // Add mock repositories that don't require Azurite
            var mockDailyLogRepo = new Mock<IDailyLogRepository>();
            mockDailyLogRepo.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DailyLogEntity?)null);
            mockDailyLogRepo.Setup(r => r.GetRangeAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DailyLogEntity>());
            mockDailyLogRepo.Setup(r => r.UpsertAsync(It.IsAny<DailyLogEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockUserRepo = new Mock<IUserRepository>();
            mockUserRepo.Setup(r => r.InitializeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockUserRepo.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntity?)null);
            mockUserRepo.Setup(r => r.UpsertAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(mockDailyLogRepo.Object);
            services.AddSingleton(mockUserRepo.Object);
        });

        // Ensure the configuration has required settings for Google OAuth
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:ClientId"] = "test-client-id",
                ["Google:ClientSecret"] = "test-client-secret",
                ["ConnectionStrings:AzureStorage"] = "UseDevelopmentStorage=true"
            });
        });
    }
}
