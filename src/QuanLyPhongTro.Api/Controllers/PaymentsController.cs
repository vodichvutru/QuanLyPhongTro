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

    /// <summary>Ghi nhận thanh toán cho hóa đơn.</summary>
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentRequest request)
        => Ok(await _payments.CreateAsync(request));

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
