namespace DistriFlow.Application.Common.Notifications;

public sealed record OrderChangedNotification(Guid OrderId, long RowVersion, string Status);
public sealed record StockChangedNotification(IReadOnlyList<Guid> ProductIds);

public interface IOperationalNotifier
{
    // Advisory delivery must not fail an already committed operation.
    Task OrderChangedAsync(Guid distributorId, Guid salesRepId, OrderChangedNotification notification);
    Task StockChangedAsync(Guid distributorId, StockChangedNotification notification);
}
