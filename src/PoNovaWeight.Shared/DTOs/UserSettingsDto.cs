namespace PoNovaWeight.Shared.DTOs;

/// <summary>
/// User settings including health goals and preferences.
/// </summary>
public record UserSettingsDto
{
    /// <summary>
    /// Target systolic BP (default: 120 mmHg).
    /// </summary>
    public int? TargetSystolic { get; init; }

    /// <summary>
    /// Target diastolic BP (default: 80 mmHg).
    /// </summary>
    public int? TargetDiastolic { get; init; }

    /// <summary>
    /// Target heart rate (default: 70 bpm).
    /// </summary>
    public int? TargetHeartRate { get; init; }

    /// <summary>
    /// Returns default settings when user hasn't configured custom goals.
    /// </summary>
    public static UserSettingsDto Default => new()
    {
        TargetSystolic = 120,
        TargetDiastolic = 80,
        TargetHeartRate = 70
    };
}
