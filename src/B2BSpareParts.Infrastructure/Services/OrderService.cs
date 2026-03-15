using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Orders;
using B2BSpareParts.Application.DTOs.Orders.ClientOrders;
using B2BSpareParts.Common;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public OrderService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PageResponse<OrderListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _db.Orders
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.OrderNumber.ToLower().Contains(search) || x.Client!.BusinessName.ToLower().Contains(search) || x.Shop!.Name.ToLower().Contains(search));
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select(x => new OrderListItemResponseDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                ShopId = x.ShopId,
                ShopName = x.Shop!.Name,
                ClientId = x.ClientId,
                ClientName = x.Client!.BusinessName,
                Status = x.Status.ToString(),
                CurrencyId = x.CurrencyId,
                CurrencyCode = x.Currency!.Code,
                TotalAmount = x.TotalAmount,
                CreatedAt = x.CreatedAt
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<PageResponse<ClientOrderListItemDto>> GetClientOrdersAsync(Guid clientId, PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Enforce that a client can only see their own orders
        if (_tenantContext.Role == UserRoles.Client)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _tenantContext.UserId && u.TenantId == tenantId && !u.IsDeleted, ct)
                ?? throw new AppException("User not found", 404);
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == user.Email && c.TenantId == tenantId && !c.IsDeleted, ct)
                ?? throw new AppException("Client profile not found", 404);

            if (client.Id != clientId)
                throw new AppException("Unauthorized to view these orders", 403);
        }

        var query = _db.Orders
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ClientId == clientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.OrderNumber.ToLower().Contains(search) || x.Shop!.Name.ToLower().Contains(search));
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select(x => new ClientOrderListItemDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                ClientId = x.ClientId,
                ShopId = x.ShopId,
                ShopName = x.Shop!.Name,
                Status = x.Status,
                CurrencyCode = x.Currency!.Code,
                Subtotal = x.Subtotal,
                DiscountAmount = x.DiscountAmount,
                TaxAmount = x.TaxAmount,
                TotalAmount = x.TotalAmount,
                CreatedAt = x.CreatedAt,
                Notes = x.Notes
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<ClientOrderSummaryDto> GetClientOrderSummaryAsync(Guid clientId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Enforce that a client can only see their own summary
        if (_tenantContext.Role == UserRoles.Client)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _tenantContext.UserId && u.TenantId == tenantId && !u.IsDeleted, ct)
                ?? throw new AppException("User not found", 404);
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == user.Email && c.TenantId == tenantId && !c.IsDeleted, ct)
                ?? throw new AppException("Client profile not found", 404);

            if (client.Id != clientId)
                throw new AppException("Unauthorized to view this summary", 403);
        }

        var orders = await _db.Orders
            .Where(x => x.TenantId == tenantId && x.ClientId == clientId && !x.IsDeleted)
            .Select(x => x.Status)
            .ToListAsync(ct);

        return new ClientOrderSummaryDto
        {
            ClientId = clientId,
            TotalOrders = orders.Count,
            CompletedOrders = orders.Count(x => x == OrderStatus.Completed),
            PendingOrders = orders.Count(x => x == OrderStatus.Pending),
            ReadyForPickupOrders = orders.Count(x => x == OrderStatus.ReadyForPickup),
            CancelledOrders = orders.Count(x => x == OrderStatus.Cancelled),
            UnableToFulfillOrders = orders.Count(x => x == OrderStatus.UnableToFulfill)
        };
    }

    public async Task<Guid> CreateAsync(CreateOrderRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var client = await _db.Clients.FirstOrDefaultAsync(x => x.Id == request.ClientId && x.TenantId == tenantId && !x.IsDeleted, ct)
                     ?? throw new AppException("Client not found", 404);
        if (client.Status != ClientStatus.Approved)
            throw new AppException("Client is not approved");

        if (request.Items.Count == 0)
            throw new AppException("Order must have at least one item");

        var products = await _db.Products.Where(x => request.Items.Select(i => i.ProductId).Contains(x.Id) && x.TenantId == tenantId && !x.IsDeleted).ToListAsync(ct);
        var order = new Order
        {
            TenantId = tenantId,
            ShopId = request.ShopId,
            ClientId = request.ClientId,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Notes = request.Notes,
            PlacedByUserId = _tenantContext.UserId
        };

        foreach (var item in request.Items)
        {
            var product = products.FirstOrDefault(x => x.Id == item.ProductId) ?? throw new AppException($"Product {item.ProductId} not found");

            // No stock is deducted on placement. We only verify there appears to be stock right now.
            if (product.TrackingType == TrackingType.Serialized)
            {
                var availableCount = await _db.SerializedInventoryUnits.CountAsync(x =>
                    x.TenantId == tenantId &&
                    x.ShopId == request.ShopId &&
                    x.ProductId == item.ProductId &&
                    x.Status == SerializedUnitStatus.InStock &&
                    !x.IsDeleted, ct);
                if (availableCount < item.Quantity)
                    throw new AppException($"Insufficient serialized stock for {product.Name}");
            }
            else
            {
                var stock = await _db.ShopInventories.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.ShopId == request.ShopId &&
                    x.ProductId == item.ProductId &&
                    !x.IsDeleted, ct);
                if (stock is null || stock.QuantityOnHand < item.Quantity)
                    throw new AppException($"Insufficient stock for {product.Name}");
            }

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.DefaultSellingPrice,
                BaseUnitPrice = product.DefaultSellingPrice,
                LineTotal = product.DefaultSellingPrice * item.Quantity
            });
        }

        order.Subtotal = order.Items.Sum(x => x.LineTotal);
        order.TotalAmount = order.Subtotal + order.TaxAmount - order.DiscountAmount;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return order.Id;
    }

    public async Task<PlaceClientOrderResponseDto> PlaceClientOrderAsync(PlaceClientOrderRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _tenantContext.UserId;

        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId && !u.IsDeleted, ct)
            ?? throw new AppException("User not found", 404);

        if (user.Role != UserRoles.Client)
            throw new AppException("Only clients can place orders through this API", 403);

        var client = await _db.Clients.FirstOrDefaultAsync(x => x.Email == user.Email && x.TenantId == tenantId && !x.IsDeleted, ct)
                     ?? throw new AppException("Client profile not found", 404);

        if (client.Status != ClientStatus.Approved)
            throw new AppException("Client is not approved", 403);

        if (request.Items == null || request.Items.Count == 0)
            throw new AppException("Order must have at least one item", 400);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(x => productIds.Contains(x.Id) && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(ct);

        var order = new Order
        {
            TenantId = tenantId,
            ShopId = request.ShopId,
            ClientId = client.Id,
            CurrencyId = client.PreferredCurrencyId ?? user.Tenant!.BaseCurrencyId,
            ExchangeRate = 1.0m, // Simplified for now, in real world we'd fetch actual rate
            OrderNumber = $"ORD-CL-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Notes = request.Notes,
            PlacedByUserId = userId,
            Status = OrderStatus.Pending
        };

        foreach (var item in request.Items)
        {
            var product = products.FirstOrDefault(x => x.Id == item.ProductId)
                          ?? throw new AppException($"Product {item.ProductId} not found", 404);

            if (!product.IsActive || !product.IsPublicVisible)
                throw new AppException($"Product {product.Name} is not available for order", 400);

            if (item.Quantity <= 0)
                throw new AppException($"Quantity for product {product.Name} must be greater than zero", 400);

            // Duplicate product IDs in the request: we could merge them or reject. Re-use existing logic of adding separate items or merging.
            // Following requirement "reject duplicate product entries or merge them" - let's merge them implicitly by checking if already added.
            var existingItem = order.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
                existingItem.LineTotal = existingItem.UnitPrice * existingItem.Quantity;
            }
            else
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.DefaultSellingPrice,
                    BaseUnitPrice = product.DefaultSellingPrice,
                    LineTotal = product.DefaultSellingPrice * item.Quantity
                });
            }
        }

        order.Subtotal = order.Items.Sum(x => x.LineTotal);
        order.TotalAmount = order.Subtotal + order.TaxAmount - order.DiscountAmount;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return new PlaceClientOrderResponseDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Message = "Order placed successfully."
        };
    }

    public async Task MarkReadyAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
                    ?? throw new AppException("Order not found", 404);

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.UnableToFulfill)
            throw new AppException("Order cannot be marked ready in its current state");

        order.Status = OrderStatus.ReadyForPickup;
        order.PreparedByUserId = _tenantContext.UserId;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.OrderReady,
            Title = "Order ready",
            Message = $"Order {order.OrderNumber} is ready for pickup.",
            RelatedEntityId = order.Id
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Order not found", 404);

        if (order.Status == OrderStatus.Completed)
            return;

        if (order.Status is OrderStatus.Cancelled or OrderStatus.UnableToFulfill)
            throw new AppException("Order cannot be completed in its current state");

        foreach (var item in order.Items)
        {
            var product = await _db.Products.FirstAsync(x => x.Id == item.ProductId && x.TenantId == tenantId && !x.IsDeleted, ct);
            if (product.TrackingType == TrackingType.Serialized)
            {
                var units = await _db.SerializedInventoryUnits
                    .Where(x => x.TenantId == tenantId && x.ShopId == order.ShopId && x.ProductId == item.ProductId && x.Status == SerializedUnitStatus.InStock && !x.IsDeleted)
                    .Take(item.Quantity)
                    .ToListAsync(ct);

                if (units.Count < item.Quantity)
                {
                    order.Status = OrderStatus.UnableToFulfill;
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                        ? $"Unable to fulfill at pickup confirmation due to insufficient serialized stock for {product.Name}."
                        : order.Notes + $" | Unable to fulfill: insufficient serialized stock for {product.Name}.";
                    order.UpdatedAt = DateTimeOffset.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    throw new AppException($"Order moved to UnableToFulfill. Insufficient serialized stock for {product.Name}.");
                }

                foreach (var unit in units)
                {
                    unit.Status = SerializedUnitStatus.Sold;
                    unit.UpdatedAt = DateTimeOffset.UtcNow;
                    _db.StockMovements.Add(new StockMovement
                    {
                        TenantId = tenantId,
                        ShopId = order.ShopId,
                        ProductId = item.ProductId,
                        SerializedInventoryUnitId = unit.Id,
                        MovementType = StockMovementType.Sale,
                        Quantity = 1,
                        ReferenceType = nameof(Order),
                        ReferenceId = order.Id,
                        PerformedByUserId = _tenantContext.UserId
                    });
                }
            }
            else
            {
                var stock = await _db.ShopInventories.Include(x => x.Product).FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId && x.ShopId == order.ShopId && x.ProductId == item.ProductId && !x.IsDeleted, ct)
                    ?? throw new AppException($"Stock not found for {product.Name}");

                if (stock.QuantityOnHand < item.Quantity)
                {
                    order.Status = OrderStatus.UnableToFulfill;
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                        ? $"Unable to fulfill at pickup confirmation due to insufficient stock for {product.Name}."
                        : order.Notes + $" | Unable to fulfill: insufficient stock for {product.Name}.";
                    order.UpdatedAt = DateTimeOffset.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    throw new AppException($"Order moved to UnableToFulfill. Insufficient stock for {product.Name}.");
                }

                stock.QuantityOnHand -= item.Quantity;
                stock.UpdatedAt = DateTimeOffset.UtcNow;

                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ShopId = order.ShopId,
                    ProductId = item.ProductId,
                    MovementType = StockMovementType.Sale,
                    Quantity = item.Quantity,
                    ReferenceType = nameof(Order),
                    ReferenceId = order.Id,
                    PerformedByUserId = _tenantContext.UserId
                });

                if (stock.QuantityOnHand <= stock.LowStockThreshold)
                {
                    _db.Notifications.Add(new Notification
                    {
                        TenantId = tenantId,
                        Type = NotificationType.LowStock,
                        Title = "Low stock alert",
                        Message = $"{product.Name} is low in stock at shop {order.ShopId}",
                        RelatedEntityId = product.Id
                    });
                }
            }
        }

        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkUnableToFulfillAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
                    ?? throw new AppException("Order not found", 404);

        if (order.Status == OrderStatus.Completed)
            throw new AppException("Completed order cannot be moved to UnableToFulfill");

        order.Status = OrderStatus.UnableToFulfill;
        order.Notes = string.IsNullOrWhiteSpace(reason)
            ? order.Notes
            : string.IsNullOrWhiteSpace(order.Notes) ? reason : order.Notes + $" | {reason}";
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
