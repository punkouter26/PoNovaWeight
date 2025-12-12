using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Query to get all daily logs for a specific month.
/// </summary>
public record GetMonthlyLogsQuery(int Year, int Month, string UserId = "dev-user") : IRequest<MonthlyLogsDto>;

/// <summary>
/// Handler for GetMonthlyLogsQuery.
/// </summary>
public class GetMonthlyLogsHandler : IRequestHandler<GetMonthlyLogsQuery, MonthlyLogsDto>
{
    private readonly IDailyLogRepository _repository;

    public GetMonthlyLogsHandler(IDailyLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<MonthlyLogsDto> Handle(GetMonthlyLogsQuery request, CancellationToken cancellationToken)
    {
        var startDate = new DateOnly(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var entities = await _repository.GetRangeAsync(
            request.UserId,
            startDate,
            endDate,
            cancellationToken);

        var days = entities.Select(e => new DailyLogSummary
        {
            Date = e.GetDate(),
            OmadCompliant = e.OmadCompliant,
            AlcoholConsumed = e.AlcoholConsumed,
            Weight = e.Weight.HasValue ? (decimal)e.Weight.Value : null
        }).ToList();

        return new MonthlyLogsDto
        {
            Year = request.Year,
            Month = request.Month,
            Days = days
        };
    }
}
