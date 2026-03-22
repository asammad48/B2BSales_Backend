using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Pos;
using B2BSpareParts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/pos")]
[Authorize(Roles = UserRoles.Owner + "," + UserRoles.Staff)]
public class PosController : ControllerBase
{
    private readonly IPosService _posService;

    public PosController(IPosService posService)
    {
        _posService = posService;
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<PageResponse<PosProductListItemDto>>>> GetProducts([FromQuery] GetPosProductsRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<PageResponse<PosProductListItemDto>>.Ok(await _posService.GetProductsAsync(request, ct)));

    [HttpPost("orders")]
    public async Task<ActionResult<ApiResponse<CreatePosOrderResponseDto>>> CreateOrder([FromBody] CreatePosOrderRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<CreatePosOrderResponseDto>.Ok(await _posService.CreateOrderAsync(request, ct)));

    [HttpGet("orders/{id:guid}/invoice-pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid id, CancellationToken ct)
    {
        var invoice = await _posService.GetInvoicePdfAsync(id, ct);
        return File(invoice.Content, invoice.ContentType, invoice.FileName);
    }
}
