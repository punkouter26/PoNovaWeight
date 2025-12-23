using MediatR;
using Microsoft.AspNetCore.Mvc;
using PoNovaWeight.Shared.DTOs;
using System.Security.Claims;

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

    private static async Task<IResult> ScanMeal(
        [FromBody] MealScanRequestDto request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.Email) ?? throw new UnauthorizedAccessException();
        var command = new ScanMealCommand(request, userId);
        var result = await mediator.Send(command, cancellationToken);

        // Return 200 even for non-success results (the success flag indicates the analysis outcome)
        return Results.Ok(result);
    }
}
