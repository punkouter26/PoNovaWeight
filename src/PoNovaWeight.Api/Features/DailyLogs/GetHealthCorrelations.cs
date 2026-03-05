using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to analyze correlations between health metrics.
/// </summary>
public record GetHealthCorrelationsQuery(int Days = 90, string UserId = "dev-user") : IRequest<HealthCorrelationsDto>;

/// <summary>
/// Handler for GetHealthCorrelationsQuery.
/// Calculates correlations between BP, weight, OMAD, and alcohol consumption.
/// </summary>
public sealed class GetHealthCorrelationsHandler(IDailyLogRepository repository, TimeProvider timeProvider)
    : IRequestHandler<GetHealthCorrelationsQuery, HealthCorrelationsDto>
{
    private const int MinimumDataPoints = 7; // Require at least 7 days for meaningful correlations
    private const int MinimumGroupSize = 5; // Minimum per group for BP comparisons

    public async Task<HealthCorrelationsDto> Handle(GetHealthCorrelationsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var startDate = today.AddDays(-request.Days + 1);

        var entities = await repository.GetRangeAsync(
            request.UserId,
            startDate,
            today,
            cancellationToken);

        var insights = new List<CorrelationInsight>();

        // OMAD vs BP correlation
        var omadBpDiff = CalculateOmadBpDifference(entities);
        if (omadBpDiff != null)
        {
            insights.AddRange(GenerateOmadInsights(omadBpDiff));
        }

        // Alcohol vs BP correlation
        var alcoholBpDiff = CalculateAlcoholBpDifference(entities);
        if (alcoholBpDiff != null)
        {
            insights.AddRange(GenerateAlcoholInsights(alcoholBpDiff));
        }

        // Weight vs BP correlation (simple Pearson correlation)
        var bpWeightCorr = CalculateBpWeightCorrelation(entities);
        if (bpWeightCorr.HasValue)
        {
            insights.AddRange(GenerateWeightCorrelationInsights(bpWeightCorr.Value));
        }

        // Add general insights
        var daysWithBp = entities.Count(e => e.SystolicBP.HasValue);
        if (daysWithBp < MinimumDataPoints)
        {
            insights.Add(new CorrelationInsight
            {
                Message = $"Log BP for at least {MinimumDataPoints} days to see meaningful patterns",
                Category = "neutral",
                Icon = "ℹ"
            });
        }

        return new HealthCorrelationsDto
        {
            Insights = insights,
            DaysAnalyzed = request.Days,
            BpWeightCorrelation = bpWeightCorr,
            OmadBpDifference = omadBpDiff,
            AlcoholBpDifference = alcoholBpDiff
        };
    }

    private static BpDifference? CalculateOmadBpDifference(IReadOnlyList<DailyLogEntity> entities)
    {
        var omadDays = entities.Where(e => e.OmadCompliant == true && e.SystolicBP.HasValue && e.DiastolicBP.HasValue).ToList();
        var nonOmadDays = entities.Where(e => e.OmadCompliant == false && e.SystolicBP.HasValue && e.DiastolicBP.HasValue).ToList();

        if (omadDays.Count < MinimumGroupSize || nonOmadDays.Count < MinimumGroupSize) return null;

        var omadAvgSys = omadDays.Average(e => e.SystolicBP!.Value);
        var omadAvgDia = omadDays.Average(e => e.DiastolicBP!.Value);
        var nonOmadAvgSys = nonOmadDays.Average(e => e.SystolicBP!.Value);
        var nonOmadAvgDia = nonOmadDays.Average(e => e.DiastolicBP!.Value);

        return new BpDifference
        {
            SystolicDifference = (decimal)(omadAvgSys - nonOmadAvgSys),
            DiastolicDifference = (decimal)(omadAvgDia - nonOmadAvgDia),
            Group1Count = omadDays.Count,
            Group2Count = nonOmadDays.Count
        };
    }

    private static BpDifference? CalculateAlcoholBpDifference(IReadOnlyList<DailyLogEntity> entities)
    {
        var alcoholDays = entities.Where(e => e.AlcoholConsumed == true && e.SystolicBP.HasValue && e.DiastolicBP.HasValue).ToList();
        var nonAlcoholDays = entities.Where(e => e.AlcoholConsumed == false && e.SystolicBP.HasValue && e.DiastolicBP.HasValue).ToList();

        if (alcoholDays.Count < MinimumGroupSize || nonAlcoholDays.Count < MinimumGroupSize) return null;

        var alcoAvgSys = alcoholDays.Average(e => e.SystolicBP!.Value);
        var alcoAvgDia = alcoholDays.Average(e => e.DiastolicBP!.Value);
        var nonAlcoAvgSys = nonAlcoholDays.Average(e => e.SystolicBP!.Value);
        var nonAlcoAvgDia = nonAlcoholDays.Average(e => e.DiastolicBP!.Value);

        return new BpDifference
        {
            SystolicDifference = (decimal)(alcoAvgSys - nonAlcoAvgSys),
            DiastolicDifference = (decimal)(alcoAvgDia - nonAlcoAvgDia),
            Group1Count = alcoholDays.Count,
            Group2Count = nonAlcoholDays.Count
        };
    }

    private static double? CalculateBpWeightCorrelation(IReadOnlyList<DailyLogEntity> entities)
    {
        var data = entities
            .Where(e => e.SystolicBP.HasValue && e.Weight.HasValue)
            .Select(e => (Bp: e.SystolicBP!.Value, Weight: e.Weight!.Value))
            .ToList();

        if (data.Count < MinimumDataPoints) return null;

        // Calculate Pearson correlation coefficient
        var avgBp = data.Average(d => d.Bp);
        var avgWeight = data.Average(d => d.Weight);
        
        var numerator = data.Sum(d => (d.Bp - avgBp) * (d.Weight - avgWeight));
        var denomBp = Math.Sqrt(data.Sum(d => Math.Pow(d.Bp - avgBp, 2)));
        var denomWeight = Math.Sqrt(data.Sum(d => Math.Pow(d.Weight - avgWeight, 2)));

        if (denomBp == 0 || denomWeight == 0) return null;

        return numerator / (denomBp * denomWeight);
    }

    private static List<CorrelationInsight> GenerateOmadInsights(BpDifference diff)
    {
        var insights = new List<CorrelationInsight>();
        var sysDiff = Math.Abs(diff.SystolicDifference);

        if (diff.SystolicDifference < -3)
        {
            insights.Add(new CorrelationInsight
            {
                Message = $"Your BP is {sysDiff:F0} points lower on OMAD days - keep it up!",
                Category = "positive",
                Icon = "✓"
            });
        }
        else if (diff.SystolicDifference > 3)
        {
            insights.Add(new CorrelationInsight
            {
                Message = $"Your BP is {sysDiff:F0} points higher on OMAD days - consider reviewing your OMAD meals",
                Category = "negative",
                Icon = "✗"
            });
        }

        return insights;
    }

    private static List<CorrelationInsight> GenerateAlcoholInsights(BpDifference diff)
    {
        var insights = new List<CorrelationInsight>();
        var sysDiff = Math.Abs(diff.SystolicDifference);

        if (diff.SystolicDifference > 5)
        {
            insights.Add(new CorrelationInsight
            {
                Message = $"Alcohol increases your BP by {sysDiff:F0} points on average - consider reducing intake",
                Category = "negative",
                Icon = "✗"
            });
        }
        else if (diff.SystolicDifference > 0)
        {
            insights.Add(new CorrelationInsight
            {
                Message = $"Alcohol slightly elevates your BP by {sysDiff:F0} points",
                Category = "neutral",
                Icon = "ℹ"
            });
        }

        return insights;
    }

    private static List<CorrelationInsight> GenerateWeightCorrelationInsights(double correlation)
    {
        var insights = new List<CorrelationInsight>();

        if (correlation > 0.5)
        {
            insights.Add(new CorrelationInsight
            {
                Message = "Strong positive correlation: Weight loss may help reduce BP",
                Category = "positive",
                Icon = "✓"
            });
        }
        else if (correlation < -0.5)
        {
            insights.Add(new CorrelationInsight
            {
                Message = "Negative correlation detected: Consult healthcare provider",
                Category = "neutral",
                Icon = "ℹ"
            });
        }

        return insights;
    }
}
