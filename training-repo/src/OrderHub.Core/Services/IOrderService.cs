using OrderHub.Core.Common;
using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

public interface IOrderService
{
    Task<PagedResult<Order>> GetOrdersAsync(int page, int pageSize, OrderStatus? status);
    Task<Order?> GetOrderAsync(int id);
    Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(int customerId);
    Task<ServiceResult<Order>> CreateOrderAsync(int customerId, IReadOnlyList<NewOrderLine> lines);
    Task<ServiceResult<Order>> CancelOrderAsync(int id);

    decimal GetDiscountRate(CustomerTier tier);
    decimal GetAppliedDiscountRate(Order order);
    decimal CalculateSubtotal(Order order);
    decimal CalculateTotal(Order order);
}
