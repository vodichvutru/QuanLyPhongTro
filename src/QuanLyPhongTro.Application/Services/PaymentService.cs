using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

public class PaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db) => _db = db;

    /// <summary>Ghi nhận thanh toán và tự cập nhật trạng thái hóa đơn (Paid / PartiallyPaid).</summary>
    public async Task<PaymentDto> CreateAsync(CreatePaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new AppException("Số tiền thanh toán phải lớn hơn 0.");

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId)
            ?? throw new AppException("Không tìm thấy hóa đơn.", 404);
        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new AppException("Không thể thanh toán cho hóa đơn đã hủy.", 409);
        if (invoice.Status == InvoiceStatus.Paid)
            throw new AppException("Hóa đơn này đã được thanh toán đủ.");

        var remaining = InvoiceCalculatorRound(invoice.TotalAmount - invoice.PaidAmount);
        if (request.Amount > remaining + 0.01m)
            throw new AppException($"Số tiền vượt quá số còn phải trả ({remaining:N0} VNĐ).");

        return await AddPaymentAsync(invoice, request.Amount, request.Method,
            request.PaidAt ?? DateTime.Now, request.Reference, request.Note);
    }

    /// <summary>
    /// Người thuê thanh toán trực tuyến (BR-07): chỉ hóa đơn của chính mình, trả đủ số còn thiếu,
    /// phương thức trực tuyến (không có tiền mặt). Ghi nhận ngay để chủ trọ đối chiếu.
    /// </summary>
    public async Task<PaymentDto> CreateByTenantAsync(int userId, int invoiceId, CreateMyPaymentRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive)
            ?? throw new AppException("Tài khoản của bạn chưa được liên kết với hồ sơ người thuê. Liên hệ chủ trọ.", 403);

        var invoice = await _db.Invoices
            .Include(i => i.Contract)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Contract.TenantId == tenant.Id)
            ?? throw new AppException("Không tìm thấy hóa đơn.", 404);
        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new AppException("Không thể thanh toán cho hóa đơn đã hủy.", 409);
        if (invoice.Status == InvoiceStatus.Paid)
            throw new AppException("Hóa đơn này đã được thanh toán đủ.");

        if (request.Method is not (PaymentMethod.BankTransfer or PaymentMethod.Momo or PaymentMethod.VnPay))
            throw new AppException("Thanh toán qua web chỉ hỗ trợ Chuyển khoản, Ví Momo hoặc Cổng VNPay.");

        var remaining = InvoiceCalculatorRound(invoice.TotalAmount - invoice.PaidAmount);
        var reference = string.IsNullOrWhiteSpace(request.Reference)
            ? $"WEB-{request.Method}-{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}"
            : request.Reference.Trim();

        return await AddPaymentAsync(invoice, remaining, request.Method, DateTime.Now, reference, request.Note);
    }

    private async Task<PaymentDto> AddPaymentAsync(Invoice invoice, decimal amount, PaymentMethod method, DateTime paidAt, string? reference, string? note)
    {
        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = amount,
            Method = method,
            PaidAt = paidAt,
            Reference = reference?.Trim(),
            Note = note?.Trim()
        };

        invoice.PaidAmount = InvoiceCalculatorRound(invoice.PaidAmount + amount);
        invoice.Status = invoice.PaidAmount >= invoice.TotalAmount - 0.01m
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return DtoMapper.ToPayment(payment);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListByInvoiceAsync(int invoiceId)
    {
        if (!await _db.Invoices.AnyAsync(i => i.Id == invoiceId))
            throw new AppException("Không tìm thấy hóa đơn.", 404);

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
            .ToListAsync();
        return payments.Select(DtoMapper.ToPayment).ToList();
    }

    public async Task<IReadOnlyList<PaymentDto>> ListRecentAsync(int count = 50)
    {
        var payments = await _db.Payments.AsNoTracking()
            .OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
            .Take(Math.Clamp(count, 1, 200))
            .ToListAsync();
        return payments.Select(DtoMapper.ToPayment).ToList();
    }

    /// <summary>Danh sách thanh toán: lọc theo hóa đơn (nếu có) hoặc toàn bộ gần nhất.</summary>
    public async Task<IReadOnlyList<PaymentDto>> ListAsync(int? invoiceId = null, int count = 200)
    {
        var q = _db.Payments.AsNoTracking().AsQueryable();
        if (invoiceId.HasValue)
            q = q.Where(p => p.InvoiceId == invoiceId.Value);
        var payments = await q
            .OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
            .Take(Math.Clamp(count, 1, 500))
            .ToListAsync();
        return payments.Select(DtoMapper.ToPayment).ToList();
    }

    public async Task<PaymentDto> GetAsync(int id)
    {
        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new AppException("Không tìm thấy khoản thanh toán.", 404);
        return DtoMapper.ToPayment(payment);
    }

    private static decimal InvoiceCalculatorRound(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
