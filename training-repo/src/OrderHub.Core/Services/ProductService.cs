using OrderHub.Core.Domain;
using OrderHub.Core.Common;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<ServiceResult<IReadOnlyList<LowStockProduct>>> GetLowStockAsync(int threshold)
    {
        if (threshold <= 0)
            return ServiceResult<IReadOnlyList<LowStockProduct>>.Fail("門檻必須大於 0");

        var now = DateTime.UtcNow;
        var products = await _productRepository.GetLowStockAsync(
            threshold, now.AddDays(-30), now, OrderStatus.Cancelled);
        return ServiceResult<IReadOnlyList<LowStockProduct>>.Ok(products);
    }
}
