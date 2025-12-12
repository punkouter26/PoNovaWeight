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
public class IncrementUnitHandler : IRequestHandler<IncrementUnitCommand, DailyLogDto>
{
    private readonly IDailyLogRepository _repository;

    public IncrementUnitHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyLogDto> Handle(IncrementUnitCommand request, CancellationToken cancellationToken)
    {
        // Get existing entity or create a new one
        var entity = await _repository.GetAsync(request.UserId, request.Request.Date, cancellationToken)
            ?? DailyLogEntity.Create(request.UserId, request.Request.Date);

        // Apply the delta to the appropriate category
        ApplyDelta(entity, request.Request.Category, request.Request.Delta);

        // Persist changes
        await _repository.UpsertAsync(entity, cancellationToken);

        return new DailyLogDto
        {
            Date = entity.GetDate(),
            Proteins = entity.Proteins,
            Vegetables = entity.Vegetables,
            Fruits = entity.Fruits,
            Starches = entity.Starches,
            Fats = entity.Fats,
            Dairy = entity.Dairy,
            WaterSegments = entity.WaterSegments
        };
    }

    private static void ApplyDelta(DailyLogEntity entity, UnitCategory category, int delta)
    {
        switch (category)
        {
            case UnitCategory.Proteins:
                entity.Proteins = Math.Max(0, entity.Proteins + delta);
                break;
            case UnitCategory.Vegetables:
                entity.Vegetables = Math.Max(0, entity.Vegetables + delta);
                break;
            case UnitCategory.Fruits:
                entity.Fruits = Math.Max(0, entity.Fruits + delta);
                break;
            case UnitCategory.Starches:
                entity.Starches = Math.Max(0, entity.Starches + delta);
                break;
            case UnitCategory.Fats:
                entity.Fats = Math.Max(0, entity.Fats + delta);
                break;
            case UnitCategory.Dairy:
                entity.Dairy = Math.Max(0, entity.Dairy + delta);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category), $"Unknown category: {category}");
        }
    }
}
