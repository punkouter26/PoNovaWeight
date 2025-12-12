using MediatR;
using PoNovaWeight.Api.Infrastructure.OpenAI;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.MealScan;

/// <summary>
/// Command to analyze a meal image using AI.
/// </summary>
public record ScanMealCommand(MealScanRequestDto Request, string UserId = "dev-user") : IRequest<MealScanResultDto>;

/// <summary>
/// Handler for ScanMealCommand.
/// </summary>
public class ScanMealHandler : IRequestHandler<ScanMealCommand, MealScanResultDto>
{
    private readonly IMealAnalysisService _analysisService;
    private readonly ILogger<ScanMealHandler> _logger;

    public ScanMealHandler(IMealAnalysisService analysisService, ILogger<ScanMealHandler> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    public async Task<MealScanResultDto> Handle(ScanMealCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scanning meal for user {UserId} on date {Date}",
            request.UserId, request.Request.Date);

        // Validate image data
        if (string.IsNullOrWhiteSpace(request.Request.ImageBase64))
        {
            return MealScanResultDto.FromError("No image data provided.");
        }

        // Basic validation of base64 format
        try
        {
            var imageBytes = Convert.FromBase64String(request.Request.ImageBase64);
            if (imageBytes.Length < 100)
            {
                return MealScanResultDto.FromError("Image data is too small or invalid.");
            }

            // Limit image size to ~10MB
            if (imageBytes.Length > 10 * 1024 * 1024)
            {
                return MealScanResultDto.FromError("Image is too large. Please use a smaller image (max 10MB).");
            }
        }
        catch (FormatException)
        {
            return MealScanResultDto.FromError("Invalid image format. Please provide a valid base64-encoded image.");
        }

        // Call AI service
        var result = await _analysisService.AnalyzeMealAsync(request.Request.ImageBase64, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Meal scan successful with confidence {Confidence}%", result.ConfidenceScore);
        }
        else
        {
            _logger.LogWarning("Meal scan failed: {Error}", result.ErrorMessage);
        }

        return result;
    }
}
