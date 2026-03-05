using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Infrastructure.OpenAI;

/// <summary>
/// Service for AI-powered blood pressure predictions using Azure OpenAI.
/// </summary>
public class BpPredictionService(
    AzureOpenAIClient client,
    IConfiguration configuration,
    ILogger<BpPredictionService> logger) : IBpPredictionService
{
    private readonly string _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

    public async Task<BpPredictionResultDto> PredictBpAsync(
        BpPredictionRequestDto request,
        string historicalDataSummary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClient = client.GetChatClient(_deploymentName);

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(request, historicalDataSummary);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f, // Slightly higher for creative insights
                MaxOutputTokenCount = 600,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

            if (response.Value is null || response.Value.Content.Count == 0)
            {
                return BpPredictionResultDto.FromError("No response from AI service.");
            }

            var jsonResponse = response.Value.Content[0].Text;
            logger.LogInformation("OpenAI BP prediction response: {JsonResponse}", jsonResponse);
            return ParsePredictionResponse(jsonResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating BP prediction");
            return BpPredictionResultDto.FromError($"Prediction failed: {ex.Message}");
        }
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are a cardiovascular health analyst AI. Based on a user's historical blood pressure data, 
            weight trends, OMAD compliance, alcohol consumption patterns, and planned lifestyle changes, 
            predict likely blood pressure outcomes.

            Provide predictions in JSON format with these fields:
            {
              "predictedBpRange": "Expected systolic/diastolic range (e.g., '118-125 / 75-82 mmHg')",
              "recommendations": ["Array of 3-5 specific, actionable recommendations"],
              "confidenceScore": 1-100 (based on data quality and amount),
              "reasoning": "Brief explanation of prediction factors"
            }

            ## Guidelines:
            - Normal BP: <120/80 mmHg
            - Elevated: 120-129/<80 mmHg
            - Stage 1 Hypertension: 130-139/80-89 mmHg
            - Stage 2 Hypertension: ≥140/90 mmHg
            - OMAD typically reduces BP by 3-8 mmHg when sustained
            - Alcohol increases BP by 2-10 mmHg temporarily
            - Weight loss of 1 lb can reduce systolic BP by ~1 mmHg
            - Consider circadian rhythms: BP naturally lower in morning

            Be cautious and evidence-based. Always recommend consulting healthcare providers for concerning readings.
            """;
    }

    private static string BuildUserPrompt(BpPredictionRequestDto request, string historicalDataSummary)
    {
        var contextParts = new List<string>
        {
            $"Historical BP Data:\n{historicalDataSummary}",
            $"\nPlanned Changes:",
            $"- OMAD: {(request.PlansOmad ? "Yes" : "No")}",
            $"- Alcohol: {(request.PlansAlcohol ? "Yes" : "No")}"
        };

        if (request.PlannedWeightChange.HasValue)
        {
            var direction = request.PlannedWeightChange.Value > 0 ? "gain" : "loss";
            contextParts.Add($"- Expected weight {direction}: {Math.Abs(request.PlannedWeightChange.Value):F1} lbs");
        }

        if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
        {
            contextParts.Add($"- Additional context: {request.AdditionalContext}");
        }

        contextParts.Add("\nPredict blood pressure trends and provide recommendations.");

        return string.Join("\n", contextParts);
    }

    private BpPredictionResultDto ParsePredictionResponse(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            var predictedBpRange = root.TryGetProperty("predictedBpRange", out var bpRange)
                ? bpRange.GetString()
                : "Unable to predict";

            var recommendations = new List<string>();
            if (root.TryGetProperty("recommendations", out var recArray) && recArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in recArray.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        recommendations.Add(item.GetString() ?? string.Empty);
                    }
                }
            }

            var confidenceScore = root.TryGetProperty("confidenceScore", out var conf)
                ? conf.GetInt32()
                : 50;

            return new BpPredictionResultDto
            {
                Success = true,
                PredictedBpRange = predictedBpRange,
                Recommendations = recommendations,
                ConfidenceScore = Math.Clamp(confidenceScore, 0, 100),
                ErrorMessage = null
            };
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse BP prediction JSON response");
            return BpPredictionResultDto.FromError("Failed to parse prediction results.");
        }
    }
}
