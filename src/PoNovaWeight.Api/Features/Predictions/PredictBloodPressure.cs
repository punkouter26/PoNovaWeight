using MediatR;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Predictions;

/// <summary>
/// Command to predict blood pressure based on planned lifestyle changes.
/// </summary>
public record PredictBloodPressureCommand(BpPredictionRequestDto Request, string UserId = "dev-user") 
    : IRequest<BpPredictionResultDto>;

/// <summary>
/// Handler for PredictBloodPressureCommand.
/// Fetches historical data and uses AI to generate predictions.
/// </summary>
public sealed class PredictBloodPressureHandler(
    IBpPredictionService predictionService,
    IDailyLogRepository repository,
    TimeProvider timeProvider,
    ILogger<PredictBloodPressureHandler> logger) 
    : IRequestHandler<PredictBloodPressureCommand, BpPredictionResultDto>
{
    private const int HistoricalDays = 90;

    public async Task<BpPredictionResultDto> Handle(
        PredictBloodPressureCommand request, 
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating BP prediction for user {UserId}", request.UserId);

        // Fetch historical data
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var startDate = today.AddDays(-HistoricalDays + 1);

        var entities = await repository.GetRangeAsync(
            request.UserId,
            startDate,
            today,
            cancellationToken);

        if (entities.Count == 0)
        {
            return BpPredictionResultDto.FromError(
                "Insufficient data for predictions. Log BP readings for at least 7 days.");
        }

        // Build historical data summary for AI
        var historicalSummary = BuildHistoricalSummary(entities);

        // Call AI service
        var result = await predictionService.PredictBpAsync(
            request.Request,
            historicalSummary,
            cancellationToken);

        if (result.Success)
        {
            logger.LogInformation("BP prediction successful with confidence {Confidence}%", result.ConfidenceScore);
        }
        else
        {
            logger.LogWarning("BP prediction failed: {Error}", result.ErrorMessage);
        }

        return result;
    }

    private static string BuildHistoricalSummary(IReadOnlyList<DailyLogEntity> entities)
    {
        var bpReadings = entities
            .Where(e => e.SystolicBP.HasValue && e.DiastolicBP.HasValue)
            .ToList();

        if (bpReadings.Count == 0)
        {
            return "No blood pressure readings available.";
        }

        var avgSystolic = bpReadings.Average(e => e.SystolicBP!.Value);
        var avgDiastolic = bpReadings.Average(e => e.DiastolicBP!.Value);
        var minSystolic = bpReadings.Min(e => e.SystolicBP!.Value);
        var maxSystolic = bpReadings.Max(e => e.SystolicBP!.Value);

        // Weight trends
        var weights = entities.Where(e => e.Weight.HasValue).ToList();
        var weightTrend = weights.Count >= 2
            ? weights.Last().Weight!.Value - weights.First().Weight!.Value
            : 0;

        // OMAD compliance
        var omadDays = entities.Count(e => e.OmadCompliant == true);
        var omadRate = entities.Count > 0 ? (double)omadDays / entities.Count * 100 : 0;

        // Alcohol consumption
        var alcoholDays = entities.Count(e => e.AlcoholConsumed == true);
        var alcoholRate = entities.Count > 0 ? (double)alcoholDays / entities.Count * 100 : 0;

        // BP on OMAD vs non-OMAD days
        var omadBpReadings = bpReadings.Where(e => e.OmadCompliant == true).ToList();
        var nonOmadBpReadings = bpReadings. Where(e => e.OmadCompliant == false).ToList();
        
        var omadAvgSys = omadBpReadings.Count > 0 
            ? omadBpReadings.Average(e => e.SystolicBP!.Value) 
            : 0;
        var nonOmadAvgSys = nonOmadBpReadings.Count > 0 
            ? nonOmadBpReadings.Average(e => e.SystolicBP!.Value) 
            : 0;

        var summary = $"""
            ## Last {HistoricalDays} Days Summary:
            - BP Readings: {bpReadings.Count} days
            - Average BP: {avgSystolic:F0}/{avgDiastolic:F0} mmHg
            - BP Range: {minSystolic:F0}-{maxSystolic:F0} systolic
            - Weight Trend: {(weightTrend > 0 ? "+" : "")}{weightTrend:F1} lbs
            - OMAD Compliance: {omadRate:F0}% ({omadDays} days)
            - Alcohol Consumption: {alcoholRate:F0}% ({alcoholDays} days)
            """;

        if (omadBpReadings.Count >= 3 && nonOmadBpReadings.Count >= 3)
        {
            var bpDiff = nonOmadAvgSys - omadAvgSys;
            summary += $"\n- BP Impact: OMAD days show {Math.Abs(bpDiff):F0} mmHg {(bpDiff > 0 ? "lower" : "higher")} systolic";
        }

        return summary;
    }
}
