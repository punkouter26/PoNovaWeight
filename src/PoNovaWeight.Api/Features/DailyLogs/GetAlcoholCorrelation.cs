using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to get alcohol correlation data showing weight differences between alcohol and non-alcohol days.
/// </summary>
public record GetAlcoholCorrelationQuery(int Days = 90, string UserId = "dev-user") : IRequest<AlcoholCorrelationDto>;

/// <summary>
/// Handler for GetAlcoholCorrelationQuery.
/// Calculates average weight on alcohol days vs non-alcohol days.
/// </summary>
public sealed class GetAlcoholCorrelationHandler(IDailyLogRepository repository, TimeProvider timeProvider) : IRequestHandler<GetAlcoholCorrelationQuery, AlcoholCorrelationDto>
{
    public async Task<AlcoholCorrelationDto> Handle(GetAlcoholCorrelationQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var startDate = today.AddDays(-request.Days + 1);

        var entities = await repository.GetRangeAsync(
            request.UserId,
            startDate,
            today,
            cancellationToken);

        // Filter to entries that have both weight and explicit alcohol status
        var validEntries = entities
            .Where(e => e.Weight.HasValue && e.AlcoholConsumed.HasValue)
            .ToList();

        var alcoholDays = validEntries.Where(e => e.AlcoholConsumed == true).ToList();
        var nonAlcoholDays = validEntries.Where(e => e.AlcoholConsumed == false).ToList();

        var alcoholWeights = alcoholDays
            .Select(e => e.Weight)
            .OfType<double>()
            .ToList();
        var nonAlcoholWeights = nonAlcoholDays
            .Select(e => e.Weight)
            .OfType<double>()
            .ToList();

        var daysWithAlcohol = alcoholWeights.Count;
        var daysWithoutAlcohol = nonAlcoholWeights.Count;

        // Need at least 1 day of each to calculate meaningful correlation
        if (daysWithAlcohol == 0 || daysWithoutAlcohol == 0)
        {
            return new AlcoholCorrelationDto
            {
                DaysWithAlcohol = daysWithAlcohol,
                DaysWithoutAlcohol = daysWithoutAlcohol,
                AverageWeightWithAlcohol = null,
                AverageWeightWithoutAlcohol = null,
                WeightDifference = null,
                HasSufficientData = false
            };
        }

        var avgWithAlcohol = (decimal)alcoholWeights.Average();
        var avgWithoutAlcohol = (decimal)nonAlcoholWeights.Average();
        var difference = avgWithAlcohol - avgWithoutAlcohol;

        return new AlcoholCorrelationDto
        {
            DaysWithAlcohol = daysWithAlcohol,
            DaysWithoutAlcohol = daysWithoutAlcohol,
            AverageWeightWithAlcohol = avgWithAlcohol,
            AverageWeightWithoutAlcohol = avgWithoutAlcohol,
            WeightDifference = difference,
            HasSufficientData = true
        };
    }
}
