using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Orders.ClientOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/client")]
public class ClientOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public ClientOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("clients/{clientId:guid}/orders")]
    public async Task<ActionResult<ApiResponse<PageResponse<ClientOrderListItemDto>>>> GetClientOrders(Guid clientId, [FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<ClientOrderListItemDto>>.Ok(await _orderService.GetClientOrdersAsync(clientId, request, ct)));

    [HttpGet("clients/{clientId:guid}/orders/summary")]
    public async Task<ActionResult<ApiResponse<ClientOrderSummaryDto>>> GetClientOrderSummary(Guid clientId, CancellationToken ct)
        => Ok(ApiResponse<ClientOrderSummaryDto>.Ok(await _orderService.GetClientOrderSummaryAsync(clientId, ct)));
}
