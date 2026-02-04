using System.Text.Json.Serialization;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Shared;

/// <summary>
/// JSON source generator context for AOT-friendly serialization.
/// Provides compile-time serialization metadata for all DTOs.
/// </summary>
[JsonSerializable(typeof(DailyLogDto))]
[JsonSerializable(typeof(WeeklySummaryDto))]
[JsonSerializable(typeof(MonthlyLogsDto))]
[JsonSerializable(typeof(DailyLogSummary))]
[JsonSerializable(typeof(StreakDto))]
[JsonSerializable(typeof(WeightTrendsDto))]
[JsonSerializable(typeof(TrendDataPoint))]
[JsonSerializable(typeof(AlcoholCorrelationDto))]
[JsonSerializable(typeof(MealScanRequestDto))]
[JsonSerializable(typeof(MealScanResultDto))]
[JsonSerializable(typeof(MealSuggestions))]
[JsonSerializable(typeof(IncrementUnitRequest))]
[JsonSerializable(typeof(UpdateWaterRequest))]
[JsonSerializable(typeof(AuthStatus))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(UnitCategory))]
[JsonSerializable(typeof(List<DailyLogDto>))]
[JsonSerializable(typeof(List<DailyLogSummary>))]
[JsonSerializable(typeof(List<TrendDataPoint>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class NovaJsonContext : JsonSerializerContext
{
}
