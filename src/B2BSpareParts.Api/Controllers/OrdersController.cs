using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("admin-orders")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Orders.OrderListItemResponseDto>>>> GetPaged([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Orders.OrderListItemResponseDto>>.Ok(await _orderService.GetPagedAsync(request, ct)));

    [HttpGet("get-order-by-id/{id:guid}")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.DTOs.Orders.OrderDetailsDto>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Orders.OrderDetailsDto>.Ok(await _orderService.GetByIdAsync(id, ct)));

    //[HttpPost("admin/admin-order-create")]
    //public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] B2BSpareParts.Application.DTOs.Orders.CreateOrderRequestDto request, CancellationToken ct)
    //    => Ok(ApiResponse<Guid>.Ok(await _orderService.CreateAsync(request, ct)));

    [HttpPost("client")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.DTOs.Orders.ClientOrders.PlaceClientOrderResponseDto>>> PlaceClientOrder([FromBody] B2BSpareParts.Application.DTOs.Orders.ClientOrders.PlaceClientOrderRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Orders.ClientOrders.PlaceClientOrderResponseDto>.Ok(await _orderService.PlaceClientOrderAsync(request, ct)));

    [HttpPost("{id:guid}/ready")]
    public async Task<ActionResult<ApiResponse<string>>> MarkReady(Guid id, CancellationToken ct)
    {
        await _orderService.MarkReadyAsync(id, ct);
        return Ok(ApiResponse<string>.Ok("Order marked ready"));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<string>>> Complete(Guid id, CancellationToken ct)
    {
        await _orderService.CompleteAsync(id, ct);
        return Ok(ApiResponse<string>.Ok("Order completed"));
    }

    [HttpPost("{id:guid}/unable-to-fulfill")]
    public async Task<ActionResult<ApiResponse<string>>> MarkUnableToFulfill(Guid id, [FromBody] UnableToFulfillRequest? request, CancellationToken ct)
    {
        await _orderService.MarkUnableToFulfillAsync(id, request?.Reason, ct);
        return Ok(ApiResponse<string>.Ok("Order marked unable to fulfill"));
    }

    public sealed class UnableToFulfillRequest
    {
        public string? Reason { get; set; }
    }
}
