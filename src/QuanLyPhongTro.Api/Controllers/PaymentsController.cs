using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Admin,Owner")]
public class PaymentsController : BaseController
{
    private readonly PaymentService _payments;

    public PaymentsController(PaymentService payments) => _payments = payments;

    /// <summary>Chủ trọ ghi nhận thanh toán cho hóa đơn (xác nhận luôn).</summary>
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentRequest request)
        => Ok(await _payments.CreateAsync(request));

    /// <summary>Các khoản người thuê báo đã trả, đang chờ chủ trọ xác nhận.</summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> Pending()
        => Ok(await _payments.ListPendingAsync());

    /// <summary>Chủ trọ xác nhận khoản người thuê báo → tính vào số đã thu.</summary>
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<PaymentDto>> Confirm(int id)
        => Ok(await _payments.ConfirmAsync(id));

    /// <summary>Chủ trọ từ chối khoản người thuê báo.</summary>
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<PaymentDto>> Reject(int id, [FromBody] RejectPaymentRequest? request)
        => Ok(await _payments.RejectAsync(id, request?.Reason));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> List([FromQuery] int? invoiceId, [FromQuery] int count = 200)
        => Ok(await _payments.ListAsync(invoiceId, count));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Get(int id) => Ok(await _payments.GetAsync(id));

    [HttpGet("invoice/{invoiceId:int}")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> ListByInvoice(int invoiceId)
        => Ok(await _payments.ListByInvoiceAsync(invoiceId));

    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> Recent([FromQuery] int count = 50)
        => Ok(await _payments.ListRecentAsync(count));
}
