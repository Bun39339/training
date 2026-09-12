using OrderHub.Core.Domain;
using OrderHub.Core.Common;

namespace OrderHub.Core.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();
    Task<ServiceResult<IReadOnlyList<LowStockProduct>>> GetLowStockAsync(int threshold);
}
