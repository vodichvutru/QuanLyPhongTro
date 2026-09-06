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

        var payment = new Payment
        {
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            Method = request.Method,
            PaidAt = request.PaidAt ?? DateTime.Now,
            Reference = request.Reference?.Trim(),
            Note = request.Note?.Trim()
        };

        invoice.PaidAmount = InvoiceCalculatorRound(invoice.PaidAmount + request.Amount);
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
