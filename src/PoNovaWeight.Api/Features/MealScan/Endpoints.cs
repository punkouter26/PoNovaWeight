using Microsoft.AspNetCore.Mvc;
using PoNovaWeight.Api.Infrastructure;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.MealScan;

/// <summary>
/// Extension methods for mapping meal scan endpoints.
/// </summary>
public static class Endpoints
{
    public static IEndpointRouteBuilder MapMealScanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meal-scan")
            .WithTags("Meal Scan");

        group.MapPost("/", ScanMeal)
            .WithName("ScanMeal")
            .WithDescription("Analyzes a meal image using AI and returns suggested unit counts.")
            .Produces<MealScanResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    // Direct handler - no MediatR overhead for single-use command
    private static async Task<IResult> ScanMeal(
        [FromBody] MealScanRequestDto request,
        HttpContext httpContext,
        IMealAnalysisService analysisService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.GetUserId();
        logger.LogInformation("Scanning meal for user {UserId} on date {Date}", userId, request.Date);

        // Validate image data
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return Results.Ok(MealScanResultDto.FromError("No image data provided."));
        }

        // Basic validation of base64 format
        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(request.ImageBase64);
        }
        catch (FormatException)
        {
            return Results.Ok(MealScanResultDto.FromError("Invalid image format. Please provide a valid base64-encoded image."));
        }

        if (imageBytes.Length < 100)
        {
            return Results.Ok(MealScanResultDto.FromError("Image data is too small or invalid."));
        }

        // Limit image size to ~10MB
        if (imageBytes.Length > 10 * 1024 * 1024)
        {
            return Results.Ok(MealScanResultDto.FromError("Image is too large. Please use a smaller image (max 10MB)."));
        }

        // Call AI service
        var result = await analysisService.AnalyzeMealAsync(request.ImageBase64, cancellationToken);

        if (result.Success)
        {
            logger.LogInformation("Meal scan successful with confidence {Confidence}%", result.ConfidenceScore);
        }
        else
        {
            logger.LogWarning("Meal scan failed: {Error}", result.ErrorMessage);
        }

        // Return 200 even for non-success results (the success flag indicates the analysis outcome)
        return Results.Ok(result);
    }
}
