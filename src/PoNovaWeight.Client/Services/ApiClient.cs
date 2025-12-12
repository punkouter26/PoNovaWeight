using System.Net.Http.Json;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Client.Services;

/// <summary>
/// HTTP client wrapper for API calls.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets the daily log for a specific date.
    /// </summary>
    public async Task<DailyLogDto?> GetDailyLogAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/daily-logs/{date:yyyy-MM-dd}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DailyLogDto>(cancellationToken);
    }

    /// <summary>
    /// Creates or updates a daily log.
    /// </summary>
    public async Task UpsertDailyLogAsync(DailyLogDto dailyLog, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("api/daily-logs", dailyLog, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Increments (or decrements) a unit count for a specific category.
    /// </summary>
    public async Task<DailyLogDto?> IncrementUnitAsync(IncrementUnitRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/daily-logs/increment", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DailyLogDto>(cancellationToken);
    }

    /// <summary>
    /// Updates water intake for a specific day.
    /// </summary>
    public async Task<DailyLogDto?> UpdateWaterAsync(UpdateWaterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/daily-logs/water", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DailyLogDto>(cancellationToken);
    }

    /// <summary>
    /// Gets the weekly summary for a date within the week.
    /// </summary>
    public async Task<WeeklySummaryDto?> GetWeeklySummaryAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/weekly-summary/{date:yyyy-MM-dd}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WeeklySummaryDto>(cancellationToken);
    }

    /// <summary>
    /// Scans a meal image and returns AI-suggested unit counts.
    /// </summary>
    public async Task<MealScanResultDto?> ScanMealAsync(MealScanRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/meal-scan", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealScanResultDto>(cancellationToken);
    }

    /// <summary>
    /// Gets the current user's authentication status and profile.
    /// </summary>
    public async Task<AuthStatus?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/auth/me", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthStatus>(cancellationToken);
    }
}
