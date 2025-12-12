using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using PoNovaWeight.Shared.Contracts;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.OpenAI;

/// <summary>
/// Service for analyzing meal images using Azure OpenAI GPT-4 Vision.
/// </summary>
public class MealAnalysisService : IMealAnalysisService
{
    private readonly AzureOpenAIClient _client;
    private readonly ILogger<MealAnalysisService> _logger;
    private readonly string _deploymentName;

    public MealAnalysisService(
        AzureOpenAIClient client,
        IConfiguration configuration,
        ILogger<MealAnalysisService> logger)
    {
        _client = client;
        _logger = logger;
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
    }

    public async Task<MealScanResultDto> AnalyzeMealAsync(string imageBase64, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var systemPrompt = BuildSystemPrompt();
            var userContent = new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart("Analyze this meal image and estimate the Nova Wellness unit counts."),
                ChatMessageContentPart.CreateImagePart(
                    BinaryData.FromBytes(Convert.FromBase64String(imageBase64)),
                    "image/jpeg")
            };

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userContent)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxOutputTokenCount = 500,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            if (response.Value is null || response.Value.Content.Count == 0)
            {
                return MealScanResultDto.FromError("No response from AI service.");
            }

            var jsonResponse = response.Value.Content[0].Text;
            return ParseAnalysisResponse(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing meal image");
            return MealScanResultDto.FromError($"Analysis failed: {ex.Message}");
        }
    }

    private static string BuildSystemPrompt()
    {
        var proteinsTarget = UnitCategoryInfo.GetDailyTarget(UnitCategory.Proteins);
        var vegetablesTarget = UnitCategoryInfo.GetDailyTarget(UnitCategory.Vegetables);
        var fruitsTarget = UnitCategoryInfo.GetDailyTarget(UnitCategory.Fruits);
        var starchesTarget = UnitCategoryInfo.GetDailyTarget(UnitCategory.Starches);
        var fatsTarget = UnitCategoryInfo.GetDailyTarget(UnitCategory.Fats);
        var dairyTarget = UnitCategoryInfo.GetDailyTarget(UnitCategory.Dairy);

        return $$"""
            You are a nutrition assistant for the Nova Wellness program. Analyze meal photos and estimate portion counts.

            ## Nova Wellness Unit Definitions:
            - **Proteins**: Palm-sized portions (meat, fish, eggs, tofu). Daily target: {{proteinsTarget}} units.
            - **Vegetables**: Fist-sized portions (any non-starchy vegetables). Daily target: {{vegetablesTarget}} units.
            - **Fruits**: Fist-sized portions (any fresh/frozen fruit). Daily target: {{fruitsTarget}} units.
            - **Starches**: Cupped-hand portions (bread, rice, pasta, potatoes). Daily target: {{starchesTarget}} units.
            - **Fats**: Thumb-sized portions (oils, butter, nuts, cheese). Daily target: {{fatsTarget}} units.
            - **Dairy**: Cupped-hand portions (milk, yogurt). Daily target: {{dairyTarget}} units.

            ## Response Format (JSON):
            {"success": true, "mealDescription": "Brief description of the meal", "confidence": 80, "proteins": 0, "vegetables": 0, "fruits": 0, "starches": 0, "fats": 0, "dairy": 0}

            ## Guidelines:
            - Be conservative with estimates (better to underestimate)
            - If image is unclear, set confidence below 50
            - If no food is visible, return success: false with error message
            - Use whole numbers only (0, 1, 2, etc.)
            - Maximum single-meal estimate: 3 units per category
            """;
    }

    private static MealScanResultDto ParseAnalysisResponse(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

            if (!success)
            {
                var errorMessage = root.TryGetProperty("error", out var errorProp)
                    ? errorProp.GetString()
                    : "Analysis could not identify food in the image.";
                return MealScanResultDto.FromError(errorMessage ?? "Unknown error");
            }

            var suggestions = new MealSuggestions
            {
                Proteins = GetIntProperty(root, "proteins"),
                Vegetables = GetIntProperty(root, "vegetables"),
                Fruits = GetIntProperty(root, "fruits"),
                Starches = GetIntProperty(root, "starches"),
                Fats = GetIntProperty(root, "fats"),
                Dairy = GetIntProperty(root, "dairy")
            };

            var description = root.TryGetProperty("mealDescription", out var descProp)
                ? descProp.GetString()
                : null;

            var confidence = GetIntProperty(root, "confidence", 80);

            return MealScanResultDto.FromSuggestions(suggestions, description, confidence);
        }
        catch (JsonException ex)
        {
            return MealScanResultDto.FromError($"Failed to parse AI response: {ex.Message}");
        }
    }

    private static int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            return prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : defaultValue;
        }
        return defaultValue;
    }
}
