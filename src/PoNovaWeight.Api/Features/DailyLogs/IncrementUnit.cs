using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Command to increment (or decrement) a unit count for a specific category.
/// </summary>
public record IncrementUnitCommand(IncrementUnitRequest Request, string UserId = "dev-user") : IRequest<DailyLogDto>;

/// <summary>
/// Handler for IncrementUnitCommand.
/// </summary>
public sealed class IncrementUnitHandler(IDailyLogRepository repository) : IRequestHandler<IncrementUnitCommand, DailyLogDto>
{
    public async Task<DailyLogDto> Handle(IncrementUnitCommand request, CancellationToken cancellationToken)
    {
        // Get existing entity or create a new one
        var entity = await repository.GetAsync(request.UserId, request.Request.Date, cancellationToken)
            ?? DailyLogEntity.Create(request.UserId, request.Request.Date);

        // Apply the delta to the appropriate category
        ApplyDelta(entity, request.Request.Category, request.Request.Delta);

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

    private const int MaxUnitsPerCategory = 100;

    private static void ApplyDelta(DailyLogEntity entity, UnitCategory category, int delta)
    {
        switch (category)
        {
            case UnitCategory.Proteins:
                entity.Proteins = Math.Clamp(entity.Proteins + delta, 0, MaxUnitsPerCategory);
                break;
            case UnitCategory.Vegetables:
                entity.Vegetables = Math.Clamp(entity.Vegetables + delta, 0, MaxUnitsPerCategory);
                break;
            case UnitCategory.Fruits:
                entity.Fruits = Math.Clamp(entity.Fruits + delta, 0, MaxUnitsPerCategory);
                break;
            case UnitCategory.Starches:
                entity.Starches = Math.Clamp(entity.Starches + delta, 0, MaxUnitsPerCategory);
                break;
            case UnitCategory.Fats:
                entity.Fats = Math.Clamp(entity.Fats + delta, 0, MaxUnitsPerCategory);
                break;
            case UnitCategory.Dairy:
                entity.Dairy = Math.Clamp(entity.Dairy + delta, 0, MaxUnitsPerCategory);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category), $"Unknown category: {category}");
        }
    }
}
