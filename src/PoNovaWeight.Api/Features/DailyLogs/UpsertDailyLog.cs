using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Command to create or update a daily log.
/// </summary>
public record UpsertDailyLogCommand(DailyLogDto DailyLog, string UserId = "dev-user") : IRequest<DailyLogDto>;

/// <summary>
/// Handler for UpsertDailyLogCommand.
/// </summary>
public class UpsertDailyLogHandler : IRequestHandler<UpsertDailyLogCommand, DailyLogDto>
{
    private readonly IDailyLogRepository _repository;

    public UpsertDailyLogHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyLogDto> Handle(UpsertDailyLogCommand request, CancellationToken cancellationToken)
    {
        var entity = DailyLogEntity.Create(request.UserId, request.DailyLog.Date);
        entity.Proteins = request.DailyLog.Proteins;
        entity.Vegetables = request.DailyLog.Vegetables;
        entity.Fruits = request.DailyLog.Fruits;
        entity.Starches = request.DailyLog.Starches;
        entity.Fats = request.DailyLog.Fats;
        entity.Dairy = request.DailyLog.Dairy;
        entity.WaterSegments = request.DailyLog.WaterSegments;

        // OMAD fields
        entity.Weight = request.DailyLog.Weight.HasValue ? (double)request.DailyLog.Weight.Value : null;
        entity.OmadCompliant = request.DailyLog.OmadCompliant;
        entity.AlcoholConsumed = request.DailyLog.AlcoholConsumed;

        await _repository.UpsertAsync(entity, cancellationToken);

        return request.DailyLog;
    }
}
