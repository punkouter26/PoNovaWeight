using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.TableStorage;

/// <summary>
/// Extension methods for mapping DailyLogEntity to DailyLogDto.
/// </summary>
public static class DailyLogMappingExtensions
{
    /// <summary>
    /// Converts a DailyLogEntity to a DailyLogDto.
    /// </summary>
    public static DailyLogDto ToDto(this DailyLogEntity entity)
    {
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
            AlcoholConsumed = entity.AlcoholConsumed,
            SystolicBP = entity.SystolicBP.HasValue ? (decimal)entity.SystolicBP.Value : null,
            DiastolicBP = entity.DiastolicBP.HasValue ? (decimal)entity.DiastolicBP.Value : null,
            HeartRate = entity.HeartRate,
            BpReadingTime = entity.BpReadingTime
        };
    }
}
