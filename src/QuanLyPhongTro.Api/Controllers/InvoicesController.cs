using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Admin,Owner")]
public class InvoicesController : BaseController
{
    private readonly InvoiceService _invoices;

    public InvoicesController(InvoiceService invoices) => _invoices = invoices;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> List([FromQuery] InvoiceStatus? status, [FromQuery] string? month)
        => Ok(await _invoices.ListAsync(status, month));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Get(int id) => Ok(await _invoices.GetAsync(id));

    /// <summary>Tạo hóa đơn tự động: truyền roomId + kỳ yyyy-MM.</summary>
    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceRequest request)
        => Ok(await _invoices.CreateAsync(request));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<InvoiceDto>> Cancel(int id) => Ok(await _invoices.CancelAsync(id));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _invoices.DeleteAsync(id);
        return NoContent();
    }
}
