using MediatR;
using PoNovaWeight.Api.Infrastructure;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Settings;

/// <summary>
/// Endpoints for user settings operations.
/// </summary>
public static class Endpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        // GET /api/settings
        group.MapGet("/", async (HttpContext httpContext, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetUserSettingsQuery(httpContext.GetUserId()), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetUserSettings")
        .WithSummary("Get user settings including health goals")
        .Produces<UserSettingsDto>(StatusCodes.Status200OK);

        // PUT /api/settings
        group.MapPut("/", async (UserSettingsDto settings, HttpContext httpContext, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpsertUserSettingsCommand(settings, httpContext.GetUserId()), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("UpsertUserSettings")
        .WithSummary("Update user settings")
        .Produces<UserSettingsDto>(StatusCodes.Status200OK);
    }
}
