using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetLowStock_FiltersStrictlyBelowThresholdAndSortsByStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        foreach (var stock in new[] { 12, 9, 10, 2 })
            TestSetup.AddProduct(db, stock: stock);

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        Assert.Equal(new[] { 2, 9 }, result.Value!.Select(p => p.StockQuantity));
        Assert.All(result.Value!, p => Assert.Equal(0, p.SoldQuantityLast30Days));
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 2, sku: "ACTIVE");
        TestSetup.AddProduct(db, stock: 1, isActive: false, sku: "INACTIVE");

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        Assert.Equal("ACTIVE", Assert.Single(result.Value!).Sku);
    }

    [Fact]
    public async Task GetLowStock_SumsRecentQuantitiesExcludingCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var product = TestSetup.AddProduct(db, stock: 3, sku: "SOLD");
        TestSetup.AddProduct(db, stock: 4, sku: "UNSOLD");
        var customer = TestSetup.AddCustomer(db);
        var now = DateTime.UtcNow;
        foreach (var (status, daysAgo, quantity) in new[]
        {
            (OrderStatus.Pending, 1, 2),
            (OrderStatus.Confirmed, 15, 3),
            (OrderStatus.Shipped, 29, 4),
            (OrderStatus.Cancelled, 1, 50),
            (OrderStatus.Confirmed, 31, 60),
            (OrderStatus.Confirmed, -1, 70)
        })
        {
            db.Orders.Add(new Order
            {
                CustomerId = customer.Id,
                Status = status,
                CreatedAt = now.AddDays(-daysAgo),
                Items = new List<OrderItem>
                {
                    new() { ProductId = product.Id, Quantity = quantity, UnitPriceSnapshot = 100m }
                }
            });
        }
        await db.SaveChangesAsync();

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        Assert.Equal(9, result.Value!.Single(p => p.Sku == "SOLD").SoldQuantityLast30Days);
        Assert.Equal(0, result.Value!.Single(p => p.Sku == "UNSOLD").SoldQuantityLast30Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLowStock_InvalidThresholdReturnsFailure(int threshold)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        var result = await service.GetLowStockAsync(threshold);

        Assert.False(result.Success);
        Assert.Contains("門檻必須大於 0", result.Errors);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }
}
