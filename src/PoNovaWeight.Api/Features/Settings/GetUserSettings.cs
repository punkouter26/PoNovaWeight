using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Settings;

/// <summary>
/// Query to get user settings.
/// </summary>
public record GetUserSettingsQuery(string UserId = "dev-user") : IRequest<UserSettingsDto>;

/// <summary>
/// Handler for GetUserSettingsQuery.
/// Returns default settings if user hasn't configured custom settings.
/// </summary>
public sealed class GetUserSettingsHandler(IUserSettingsRepository repository) 
    : IRequestHandler<GetUserSettingsQuery, UserSettingsDto>
{
    public async Task<UserSettingsDto> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetAsync(request.UserId, cancellationToken);

        if (entity == null)
        {
            return UserSettingsDto.Default;
        }

        return new UserSettingsDto
        {
            TargetSystolic = entity.TargetSystolic,
            TargetDiastolic = entity.TargetDiastolic,
            TargetHeartRate = entity.TargetHeartRate
        };
    }
}
