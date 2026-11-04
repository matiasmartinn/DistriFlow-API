using System.Collections.Concurrent;
using System.Security.Claims;
using DistriFlow.Api.Security;
using DistriFlow.Application.Common.Notifications;
using DistriFlow.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DistriFlow.Api.Realtime;

public interface IOperationsClient
{
    Task OrderChanged(OrderChangedNotification notification);
    Task StockChanged(StockChangedNotification notification);
}

public sealed record OperationalConnection(Guid DistributorId, Guid UserId, string Role, Action Abort);

// One API instance. Shared routing is required before deploying multiple replicas.
public sealed class OperationalConnections
{
    private readonly ConcurrentDictionary<string, OperationalConnection> _connections = new();
    public void Add(string id, OperationalConnection connection) => _connections[id] = connection;
    public void Remove(string id) => _connections.TryRemove(id, out _);
    public KeyValuePair<string, OperationalConnection>[] Snapshot() => _connections.ToArray();
}

[Authorize(Policy = AuthorizationPolicies.ActiveDistributor)]
public sealed class OperationsHub(OperationalConnections connections) : Hub<IOperationsClient>
{
    public override async Task OnConnectedAsync()
    {
        var principal = Context.User!;
        connections.Add(Context.ConnectionId, new OperationalConnection(
            Guid.Parse(principal.FindFirstValue(DistriFlowClaims.DistributorId)!),
            Guid.Parse(principal.FindFirstValue(DistriFlowClaims.UserId)!),
            principal.FindFirstValue(DistriFlowClaims.Role)!, Context.Abort));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        connections.Remove(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
