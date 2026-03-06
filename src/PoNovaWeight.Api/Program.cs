using PoNovaWeight.Api.Infrastructure;
using Serilog;

// Configure Serilog before building the host
// Bootstrap logger: Console only (no File sink to avoid issues with read-only
// WEBSITE_RUN_FROM_PACKAGE filesystem mounts in Azure App Service).
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureNovaKeyVault();

    // Serilog configuration
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddNovaApiServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    app.UseNovaPipeline();
    app.MapNovaEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Partial class for WebApplicationFactory integration tests.
/// </summary>
public partial class Program { }
