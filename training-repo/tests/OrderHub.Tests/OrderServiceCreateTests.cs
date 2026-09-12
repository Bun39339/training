using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class OrderServiceCreateTests
{
    [Fact]
    public async Task CreateOrder_MixedLines_PreservesErrorOrderAndUnsavedStockChanges()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var firstValid = TestSetup.AddProduct(db, stock: 10);
        var insufficient = TestSetup.AddProduct(db, stock: 1);
        var lastValid = TestSetup.AddProduct(db, stock: 8);

        var result = await service.CreateOrderAsync(customer.Id, new[]
        {
            new NewOrderLine(firstValid.Id, 2),
            new NewOrderLine(insufficient.Id, 3),
            new NewOrderLine(999, 1),
            new NewOrderLine(lastValid.Id, 3)
        });

        Assert.False(result.Success);
        Assert.Equal(new[]
        {
            $"商品「{insufficient.Name}」庫存不足（現有 1，需求 3）",
            "商品（Id=999）不存在或已停售"
        }, result.Errors);
        Assert.Equal(8, firstValid.StockQuantity);
        Assert.Equal(1, insufficient.StockQuantity);
        Assert.Equal(5, lastValid.StockQuantity);
        Assert.Empty(db.Orders);

        db.ChangeTracker.Clear();
        Assert.Equal(10, db.Products.Single(p => p.Id == firstValid.Id).StockQuantity);
        Assert.Equal(8, db.Products.Single(p => p.Id == lastValid.Id).StockQuantity);
    }

    [Fact]
    public async Task CreateOrder_InvalidCustomerAndLines_ReturnsCustomerErrorFirst()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var result = await service.CreateOrderAsync(999, null!);

        Assert.False(result.Success);
        Assert.Equal("找不到指定的客戶", Assert.Single(result.Errors));
    }

    [Fact]
    public async Task CreateOrder_NonPositiveQuantityAndDuplicateProduct_ReturnsQuantityErrorFirst()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db);

        var result = await service.CreateOrderAsync(customer.Id, new[]
        {
            new NewOrderLine(product.Id, 0),
            new NewOrderLine(product.Id, 1)
        });

        Assert.False(result.Success);
        Assert.Equal("商品數量必須大於 0", Assert.Single(result.Errors));
    }

    [Fact]
    public async Task CreateOrder_HappyPath_CreatesPendingOrder()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 2) });

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(OrderStatus.Pending, result.Value!.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, db.Orders.Count());
    }

    [Fact]
    public async Task CreateOrder_SnapshotsCurrentUnitPrice()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, unitPrice: 380m);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });

        Assert.True(result.Success);
        Assert.Equal(380m, result.Value!.Items.Single().UnitPriceSnapshot);
    }

    [Fact]
    public async Task CreateOrder_GoldCustomer_PersistedTotalAppliesDiscountOnce()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db, CustomerTier.Gold);
        var product = TestSetup.AddProduct(db, unitPrice: 1420m);

        var result = await service.CreateOrderAsync(
            customer.Id,
            new[] { new NewOrderLine(product.Id, 1) });

        Assert.True(result.Success);

        db.ChangeTracker.Clear();
        var persistedOrder = await service.GetOrderAsync(result.Value!.Id);

        Assert.NotNull(persistedOrder);
        Assert.Equal(1420m, persistedOrder!.Items.Single().UnitPriceSnapshot);
        Assert.Equal(0.10m, persistedOrder.DiscountRateSnapshot);
        Assert.Equal(1278m, persistedOrder.TotalAmountSnapshot);
        Assert.Equal(1278m, service.CalculateTotal(persistedOrder!));
    }

    [Fact]
    public async Task CreateOrder_PricingSnapshotDoesNotChangeWhenCustomerTierChanges()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db, CustomerTier.Gold);
        var product = TestSetup.AddProduct(db, unitPrice: 1000m);

        var result = await service.CreateOrderAsync(
            customer.Id,
            new[] { new NewOrderLine(product.Id, 1) });

        customer.Tier = CustomerTier.Standard;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persistedOrder = await service.GetOrderAsync(result.Value!.Id);

        Assert.NotNull(persistedOrder);
        Assert.Equal(0.10m, service.GetAppliedDiscountRate(persistedOrder!));
        Assert.Equal(900m, service.CalculateTotal(persistedOrder!));
    }

    [Fact]
    public async Task CreateOrder_DecrementsProductStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 10);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 3) });

        Assert.True(result.Success);
        Assert.Equal(7, db.Products.Single(p => p.Id == product.Id).StockQuantity);
    }

    [Fact]
    public async Task CreateOrder_UnknownCustomer_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var product = TestSetup.AddProduct(db);

        var result = await service.CreateOrderAsync(999, new[] { new NewOrderLine(product.Id, 1) });

        Assert.False(result.Success);
        Assert.Contains("客戶", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrder_EmptyLines_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);

        var result = await service.CreateOrderAsync(customer.Id, Array.Empty<NewOrderLine>());

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateOrder_NonPositiveQuantity_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 0) });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateOrder_DuplicateProduct_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db);

        var result = await service.CreateOrderAsync(customer.Id, new[]
        {
            new NewOrderLine(product.Id, 1),
            new NewOrderLine(product.Id, 2)
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateOrder_InactiveProduct_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, isActive: false);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_FailsWithMessage()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 5) });

        Assert.False(result.Success);
        Assert.Contains("庫存不足", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrder_Failed_DoesNotPersistOrder()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2);

        await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 5) });

        Assert.Equal(0, db.Orders.Count());
    }
}
