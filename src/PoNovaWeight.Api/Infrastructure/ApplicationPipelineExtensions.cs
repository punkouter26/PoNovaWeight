using PoNovaWeight.Api.Features.Auth;
using PoNovaWeight.Api.Features.DailyLogs;
using PoNovaWeight.Api.Features.MealScan;
using PoNovaWeight.Api.Features.Settings;
using PoNovaWeight.Api.Features.WeeklySummary;
using Scalar.AspNetCore;
using Serilog;

namespace PoNovaWeight.Api.Infrastructure;

public static class ApplicationPipelineExtensions
{
    public static WebApplication UseNovaPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Session must be available before downstream middleware reads it.
        app.UseSession();

        app.Use(async (context, next) =>
        {
            var userId = context.User.Identity?.Name ?? "Anonymous";
            var sessionId = context.Session?.Id ?? context.Request.Cookies["ASP.NET_SessionId"] ?? "NoSession";

            using (Serilog.Context.LogContext.PushProperty("UserId", userId))
            using (Serilog.Context.LogContext.PushProperty("SessionId", sessionId))
            using (Serilog.Context.LogContext.PushProperty("Environment", app.Environment.EnvironmentName))
            {
                await next();
            }
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.UseWebAssemblyDebugging();
        }

        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseOutputCache();

        return app;
    }

    public static WebApplication MapNovaEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive");

        app.MapAuthEndpoints(app.Environment);
        app.MapDailyLogEndpoints();
        app.MapSettingsEndpoints();
        app.MapWeeklySummaryEndpoints();
        app.MapMealScanEndpoints();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        return app;
    }
}