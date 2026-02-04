using MediatR;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Features.DailyLogs;

/// <summary>
/// Command to delete a daily log entry.
/// </summary>
public record DeleteDailyLogCommand(DateOnly Date, string UserId = "dev-user") : IRequest<bool>;

/// <summary>
/// Handler for DeleteDailyLogCommand.
/// Returns true if the entry was deleted, false if not found.
/// </summary>
public sealed class DeleteDailyLogHandler(IDailyLogRepository repository) : IRequestHandler<DeleteDailyLogCommand, bool>
{
    public async Task<bool> Handle(DeleteDailyLogCommand request, CancellationToken cancellationToken)
    {
        return await repository.DeleteAsync(request.UserId, request.Date, cancellationToken);
    }
}
