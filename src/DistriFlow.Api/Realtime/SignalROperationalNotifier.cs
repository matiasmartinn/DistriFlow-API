using DistriFlow.Application.Common.Notifications;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Distributors.Enums;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Users.Enums;
using DistriFlow.Domain.Users.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace DistriFlow.Api.Realtime;

public sealed class SignalROperationalNotifier(
    IHubContext<OperationsHub, IOperationsClient> hub,
    OperationalConnections connections,
    IServiceScopeFactory scopeFactory,
    ILogger<SignalROperationalNotifier> logger) : IOperationalNotifier
{
    public Task OrderChangedAsync(Guid distributorId, Guid salesRepId, OrderChangedNotification notification) =>
        SendAsync(distributorId, connection => connection.Role == "Administrator" || connection.UserId == salesRepId,
            client => client.OrderChanged(notification));

    public Task StockChangedAsync(Guid distributorId, StockChangedNotification notification) =>
        SendAsync(distributorId, _ => true, client => client.StockChanged(notification));

    private async Task SendAsync(Guid distributorId, Func<OperationalConnection, bool> canReceive,
        Func<IOperationsClient, Task> send)
    {
        try
        {
            var candidates = connections.Snapshot().Where(item => item.Value.DistributorId == distributorId).ToArray();
            if (candidates.Length == 0) return;

            // Recheck persisted access in a fresh scope before sending to long-lived connections.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await using var scope = scopeFactory.CreateAsyncScope();
            var tenantId = DistributorId.From(distributorId);
            var distributor = await scope.ServiceProvider.GetRequiredService<IDistributorRepository>()
                .GetByIdAsync(tenantId, timeout.Token);
            var users = await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetByIdsAsync(
                tenantId, candidates.Select(item => UserId.From(item.Value.UserId)).Distinct().ToArray(), timeout.Token);
            var currentUsers = users.ToDictionary(user => user.Id.Value);
            var recipients = new List<string>();
            foreach (var (id, connection) in candidates)
            {
                if (distributor?.Status != DistributorStatus.Active
                    || !currentUsers.TryGetValue(connection.UserId, out var user)
                    || user.Status != UserStatus.Active || user.Role.Value != connection.Role)
                {
                    connections.Remove(id);
                    connection.Abort();
                    continue;
                }
                if (canReceive(connection)) recipients.Add(id);
            }
            if (recipients.Count > 0)
                await send(hub.Clients.Clients(recipients)).WaitAsync(timeout.Token);
        }
        catch (Exception exception)
        {
            // Persistence succeeded: notification failures are logged without failing the operation.
            logger.LogWarning(exception, "Could not publish operational update for distributor {DistributorId}", distributorId);
        }
    }
}
