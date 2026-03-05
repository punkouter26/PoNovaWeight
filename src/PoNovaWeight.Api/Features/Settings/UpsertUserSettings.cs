using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Features.Settings;

/// <summary>
/// Command to upsert user settings.
/// </summary>
public record UpsertUserSettingsCommand(UserSettingsDto Settings, string UserId = "dev-user") : IRequest<UserSettingsDto>;

/// <summary>
/// Handler for UpsertUserSettingsCommand.
/// </summary>
public sealed class UpsertUserSettingsHandler(IUserSettingsRepository repository) 
    : IRequestHandler<UpsertUserSettingsCommand, UserSettingsDto>
{
    public async Task<UserSettingsDto> Handle(UpsertUserSettingsCommand request, CancellationToken cancellationToken)
    {
        var entity = UserSettingsEntity.Create(request.UserId);
        entity.TargetSystolic = request.Settings.TargetSystolic;
        entity.TargetDiastolic = request.Settings.TargetDiastolic;
        entity.TargetHeartRate = request.Settings.TargetHeartRate;

        await repository.UpsertAsync(entity, cancellationToken);

        return request.Settings;
    }
}
