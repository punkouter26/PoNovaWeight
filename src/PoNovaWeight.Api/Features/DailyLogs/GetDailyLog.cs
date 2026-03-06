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
public sealed class GetDailyLogHandler(IDailyLogRepository repository) : IRequestHandler<GetDailyLogQuery, DailyLogDto?>
{
    public async Task<DailyLogDto?> Handle(GetDailyLogQuery request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetAsync(request.UserId, request.Date, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return entity.ToDto();
    }
}
