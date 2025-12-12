using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to get a daily log for a specific date.
/// </summary>
public record GetDailyLogQuery(DateOnly Date, string UserId = "dev-user") : IRequest<DailyLogDto?>;

/// <summary>
/// Handler for GetDailyLogQuery.
/// </summary>
public class GetDailyLogHandler : IRequestHandler<GetDailyLogQuery, DailyLogDto?>
{
    private readonly IDailyLogRepository _repository;

    public GetDailyLogHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyLogDto?> Handle(GetDailyLogQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.UserId, request.Date, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new DailyLogDto
        {
            Date = entity.GetDate(),
            Proteins = entity.Proteins,
            Vegetables = entity.Vegetables,
            Fruits = entity.Fruits,
            Starches = entity.Starches,
            Fats = entity.Fats,
            Dairy = entity.Dairy,
            WaterSegments = entity.WaterSegments,
            Weight = entity.Weight.HasValue ? (decimal)entity.Weight.Value : null,
            OmadCompliant = entity.OmadCompliant,
            AlcoholConsumed = entity.AlcoholConsumed
        };
    }
}
