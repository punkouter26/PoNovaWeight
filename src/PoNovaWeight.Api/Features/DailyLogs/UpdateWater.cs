using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Command to update water intake for a specific day.
/// </summary>
public record UpdateWaterCommand(UpdateWaterRequest Request, string UserId = "dev-user") : IRequest<DailyLogDto>;

/// <summary>
/// Handler for UpdateWaterCommand.
/// </summary>
public sealed class UpdateWaterHandler(IDailyLogRepository repository) : IRequestHandler<UpdateWaterCommand, DailyLogDto>
{
    public async Task<DailyLogDto> Handle(UpdateWaterCommand request, CancellationToken cancellationToken)
    {
        // Validate water segments range
        if (request.Request.Segments < 0 || request.Request.Segments > UnitCategoryInfo.WaterTargetSegments)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"Water segments must be between 0 and {UnitCategoryInfo.WaterTargetSegments}");
        }

        // Get existing entity or create a new one
        var entity = await repository.GetAsync(request.UserId, request.Request.Date, cancellationToken)
            ?? DailyLogEntity.Create(request.UserId, request.Request.Date);

        // Update water segments
        entity.WaterSegments = request.Request.Segments;

        // Persist changes
        await repository.UpsertAsync(entity, cancellationToken);

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
