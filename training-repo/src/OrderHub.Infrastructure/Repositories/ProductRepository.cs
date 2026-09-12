using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(
        int threshold, DateTime fromUtc, DateTime toUtc, OrderStatus excludedStatus)
    {
        var sales = from item in _db.OrderItems
                    join order in _db.Orders on item.OrderId equals order.Id
                    where order.CreatedAt >= fromUtc && order.CreatedAt <= toUtc
                        && order.Status != excludedStatus
                    group item by item.ProductId into items
                    select new
                    {
                        ProductId = items.Key,
                        Quantity = (int?)items.Sum(item => item.Quantity)
                    };

        var query = from product in _db.Products.AsNoTracking()
                    where product.IsActive && product.StockQuantity < threshold
                    join sale in sales on product.Id equals sale.ProductId into productSales
                    from sale in productSales.DefaultIfEmpty()
                    orderby product.StockQuantity, product.Sku
                    select new LowStockProduct
                    {
                        Sku = product.Sku,
                        Name = product.Name,
                        StockQuantity = product.StockQuantity,
                        SoldQuantityLast30Days = sale.Quantity ?? 0
                    };

        return await query.ToListAsync();
    }
}
