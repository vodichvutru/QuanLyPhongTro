using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

/// <summary>Cổng tự phục vụ dành cho người thuê (tenant).</summary>
[ApiController]
[Route("api/me")]
[Authorize(Roles = "Tenant")]
public class MeController : BaseController
{
    private readonly MeService _me;
    private readonly RepairRequestService _repairs;

    public MeController(MeService me, RepairRequestService repairs)
    {
        _me = me;
        _repairs = repairs;
    }

    [HttpGet("contracts")]
    public async Task<ActionResult<IReadOnlyList<ContractDto>>> MyContracts()
        => Ok(await _me.GetMyContractsAsync(CurrentUserId));

    [HttpGet("invoices")]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> MyInvoices()
        => Ok(await _me.GetMyInvoicesAsync(CurrentUserId));

    [HttpGet("invoices/{id:int}")]
    public async Task<ActionResult<InvoiceDto>> MyInvoice(int id)
        => Ok(await _me.GetMyInvoiceAsync(CurrentUserId, id));

    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> MyPayments()
        => Ok(await _me.GetMyPaymentsAsync(CurrentUserId));

    // ---- Yêu cầu sửa chữa của người thuê ----

    [HttpGet("repair-requests")]
    public async Task<ActionResult<IReadOnlyList<RepairRequestDto>>> MyRepairRequests()
        => Ok(await _repairs.ListMineAsync(CurrentUserId));

    [HttpPost("repair-requests")]
    public async Task<ActionResult<RepairRequestDto>> CreateRepairRequest([FromBody] CreateMyRepairRequestRequest request)
        => Ok(await _repairs.CreateMineAsync(CurrentUserId, request));

    [HttpGet("repair-requests/{id:int}")]
    public async Task<ActionResult<RepairRequestDto>> MyRepairRequest(int id)
        => Ok(await _repairs.GetMineAsync(CurrentUserId, id));

    [HttpPost("repair-requests/{id:int}/cancel")]
    public async Task<ActionResult<RepairRequestDto>> CancelRepairRequest(int id)
        => Ok(await _repairs.CancelMineAsync(CurrentUserId, id));
}
