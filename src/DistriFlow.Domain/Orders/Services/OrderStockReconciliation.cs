using DistriFlow.Domain.StockMovements;
using DistriFlow.Domain.StockMovements.Enums;

namespace DistriFlow.Domain.Orders.Services;

public static class OrderStockReconciliation
{
    public static bool MatchesConfirmation(
        Order order,
        IReadOnlyList<StockMovement> exits)
    {
        if (exits.Count != order.OrderLines.Count)
            return false;

        return order.OrderLines.All(line =>
            exits.Count(exit =>
                exit.Type == StockMovementType.Exit
                && exit.ProductId == line.ProductId
                && exit.Quantity == line.Quantity.Amount
                && exit.SaleUnit == line.Quantity.Unit) == 1);
    }
}