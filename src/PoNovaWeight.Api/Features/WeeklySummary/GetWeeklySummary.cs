using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;
using PoNovaWeight.Shared.Validation;

namespace PoNovaWeight.Api.Features.WeeklySummary;

/// <summary>
/// Query to get the weekly summary for a date within the week.
/// </summary>
public record GetWeeklySummaryQuery(DateOnly Date, string UserId = "dev-user") : IRequest<WeeklySummaryDto>;

/// <summary>
/// Handler for GetWeeklySummaryQuery.
/// Uses HybridCache to reduce Table Storage calls for repeated requests.
/// </summary>
public sealed class GetWeeklySummaryHandler(IDailyLogRepository repository, HybridCache cache) : IRequestHandler<GetWeeklySummaryQuery, WeeklySummaryDto>
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    public async Task<WeeklySummaryDto> Handle(GetWeeklySummaryQuery request, CancellationToken cancellationToken)
    {
        // Calculate week boundaries (Sunday to Saturday)
        var (weekStart, weekEnd) = DailyLogDtoValidator.GetWeekBounds(request.Date);

        // Cache key based on user and week start
        var cacheKey = $"weekly-summary:{request.UserId}:{weekStart:yyyy-MM-dd}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async ct => await FetchWeeklySummaryAsync(request.UserId, weekStart, weekEnd, ct),
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    private async Task<WeeklySummaryDto> FetchWeeklySummaryAsync(
        string userId, DateOnly weekStart, DateOnly weekEnd, CancellationToken cancellationToken)
    {
        // Fetch all daily logs for the week
        var entities = await repository.GetRangeAsync(userId, weekStart, weekEnd, cancellationToken);

        // Create a dictionary for quick lookup
        var logsByDate = entities.ToDictionary(e => e.GetDate());

        // Build the list of daily logs for the week (7 days)
        var days = new List<DailyLogDto>();
        for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
        {
            if (logsByDate.TryGetValue(date, out var entity))
            {
                days.Add(new DailyLogDto
                {
                    Date = entity.GetDate(),
                    Proteins = entity.Proteins,
                    Vegetables = entity.Vegetables,
                    Fruits = entity.Fruits,
                    Starches = entity.Starches,
                    Fats = entity.Fats,
                    Dairy = entity.Dairy,
                    WaterSegments = entity.WaterSegments
                });
            }
            else
            {
                days.Add(DailyLogDto.Empty(date));
            }
        }

        return new WeeklySummaryDto
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Days = days
        };
    }
}
