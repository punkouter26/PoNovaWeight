using MediatR;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to fetch all dashboard analytics in a single request.
/// Internally calls Weight Trends, Alcohol Correlation, and Health Correlations queries.
/// </summary>
public record GetDashboardAnalyticsQuery(int Days = 30, string UserId = "dev-user") : IRequest<DashboardAnalyticsDto>;

/// <summary>
/// Handler for GetDashboardAnalyticsQuery.
/// Consolidates multiple analytics queries into a single response to reduce API calls.
/// </summary>
public sealed class GetDashboardAnalyticsHandler(
    IMediator mediator,
    ILogger<GetDashboardAnalyticsHandler> logger) 
    : IRequestHandler<GetDashboardAnalyticsQuery, DashboardAnalyticsDto>
{
    public async Task<DashboardAnalyticsDto> Handle(
        GetDashboardAnalyticsQuery request, 
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching dashboard analytics for user {UserId}", request.UserId);

        // Fetch all analytics in parallel
        var weightTrendsTask = mediator.Send(
            new GetWeightTrendsQuery(request.Days, request.UserId), 
            cancellationToken);
        
        var alcoholCorrelationTask = mediator.Send(
            new GetAlcoholCorrelationQuery(90, request.UserId),  // Longer window for better correlation
            cancellationToken);
        
        var healthCorrelationsTask = mediator.Send(
            new GetHealthCorrelationsQuery(90, request.UserId),  // Longer window for better correlations
            cancellationToken);

        await Task.WhenAll(weightTrendsTask, alcoholCorrelationTask, healthCorrelationsTask);

        return new DashboardAnalyticsDto
        {
            WeightTrends = await weightTrendsTask,
            AlcoholCorrelation = await alcoholCorrelationTask,
            HealthCorrelations = await healthCorrelationsTask
        };
    }
}
