using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Inventory.InventoryListItemResponseDto>>>> GetPaged([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Inventory.InventoryListItemResponseDto>>.Ok(await _inventoryService.GetPagedAsync(request, ct)));

    [HttpPost("stock-in")]
    public async Task<ActionResult<ApiResponse<string>>> StockIn([FromBody] B2BSpareParts.Application.DTOs.Inventory.StockInRequestDto request, CancellationToken ct)
    {
        await _inventoryService.StockInAsync(request, ct);
        return Ok(ApiResponse<string>.Ok("Stock added"));
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<ApiResponse<string>>> Adjust([FromBody] B2BSpareParts.Application.DTOs.Inventory.AdjustStockRequestDto request, CancellationToken ct)
    {
        await _inventoryService.AdjustAsync(request, ct);
        return Ok(ApiResponse<string>.Ok("Stock adjusted"));
    }

    [HttpPost("transfers")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTransfer([FromBody] B2BSpareParts.Application.DTOs.Inventory.CreateStockTransferRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await _inventoryService.CreateTransferAsync(request, ct)));

    [HttpGet("transfers")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Inventory.StockTransferListItemResponseDto>>>> GetTransfers([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Inventory.StockTransferListItemResponseDto>>.Ok(await _inventoryService.GetTransfersAsync(request, ct)));

    [HttpPost("transfers/{id:guid}/dispatch")]
    public async Task<ActionResult<ApiResponse<string>>> Dispatch(Guid id, [FromBody] B2BSpareParts.Application.DTOs.Inventory.ProcessStockTransferRequestDto? request, CancellationToken ct)
    {
        await _inventoryService.DispatchTransferAsync(id, request, ct);
        return Ok(ApiResponse<string>.Ok("Transfer dispatched"));
    }

    [HttpPost("transfers/{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<string>>> Receive(Guid id, [FromBody] B2BSpareParts.Application.DTOs.Inventory.ProcessStockTransferRequestDto? request, CancellationToken ct)
    {
        await _inventoryService.ReceiveTransferAsync(id, request, ct);
        return Ok(ApiResponse<string>.Ok("Transfer received"));
    }
}
