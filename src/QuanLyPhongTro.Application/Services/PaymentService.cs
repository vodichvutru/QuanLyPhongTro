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
    private readonly NotificationService _notifications;

    public PaymentService(AppDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    /// <summary>
    /// Chủ trọ ghi nhận thanh toán trực tiếp (tiền mặt/chuyển khoản) → xác nhận luôn,
    /// tính ngay vào số đã thu của hóa đơn.
    /// </summary>
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

        var remaining = Round(invoice.TotalAmount - invoice.PaidAmount);
        if (request.Amount > remaining + 0.01m)
            throw new AppException($"Số tiền vượt quá số còn phải trả ({remaining:N0} VNĐ).");

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Confirmed,
            ConfirmedAt = DateTime.Now,
            PaidAt = request.PaidAt ?? DateTime.Now,
            Reference = request.Reference?.Trim(),
            Note = request.Note?.Trim()
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        await RecomputeInvoiceAsync(invoice);
        await _db.SaveChangesAsync();
        return DtoMapper.ToPayment(payment);
    }

    /// <summary>
    /// Người thuê báo đã thanh toán: tạo khoản **Chờ xác nhận** (chưa tính vào số đã thu)
    /// và gửi **thông báo cho chủ trọ** để kiểm tra, xác nhận.
    /// </summary>
    public async Task<PaymentDto> CreateByTenantAsync(int userId, int invoiceId, CreateMyPaymentRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive)
            ?? throw new AppException("Tài khoản của bạn chưa được liên kết với hồ sơ người thuê. Liên hệ chủ trọ.", 403);

        var invoice = await _db.Invoices
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Contract.TenantId == tenant.Id)
            ?? throw new AppException("Không tìm thấy hóa đơn.", 404);
        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new AppException("Không thể thanh toán cho hóa đơn đã hủy.", 409);
        if (invoice.Status == InvoiceStatus.Paid)
            throw new AppException("Hóa đơn này đã được thanh toán đủ.");

        if (request.Method is not (PaymentMethod.BankTransfer or PaymentMethod.Momo or PaymentMethod.VnPay))
            throw new AppException("Thanh toán qua web chỉ hỗ trợ Chuyển khoản, Ví Momo hoặc Cổng VNPay.");

        var pending = await _db.Payments
            .Where(p => p.InvoiceId == invoice.Id && p.Status == PaymentStatus.Pending)
            .SumAsync(p => p.Amount);
        var remaining = Round(invoice.TotalAmount - invoice.PaidAmount - pending);
        if (remaining <= 0)
            throw new AppException("Hóa đơn này đang chờ chủ trọ xác nhận thanh toán. Vui lòng chờ.", 409);

        var reference = string.IsNullOrWhiteSpace(request.Reference)
            ? $"WEB-{request.Method}-{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}"
            : request.Reference.Trim();

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = remaining,
            Method = request.Method,
            Status = PaymentStatus.Pending,
            PaidAt = DateTime.Now,
            Reference = reference,
            Note = request.Note?.Trim()
        };
        _db.Payments.Add(payment);

        _notifications.Add(
            targetRole: "Owner",
            type: "payment.pending",
            title: "Người thuê báo đã thanh toán",
            message: $"{tenant.FullName} (phòng {invoice.Contract.Room.Name}) báo đã trả {remaining:N0} ₫ cho hóa đơn {invoice.InvoiceCode}. Vào Hóa đơn kiểm tra và xác nhận.",
            linkPath: "/invoices",
            actorUserId: userId);

        await _db.SaveChangesAsync();
        return DtoMapper.ToPayment(payment);
    }

    /// <summary>Chủ trọ xác nhận khoản người thuê báo → tính vào số đã thu + báo lại cho người thuê.</summary>
    public async Task<PaymentDto> ConfirmAsync(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Contract)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new AppException("Không tìm thấy khoản thanh toán.", 404);
        if (payment.Status == PaymentStatus.Confirmed)
            throw new AppException("Khoản thanh toán này đã được xác nhận.", 409);
        if (payment.Status == PaymentStatus.Rejected)
            throw new AppException("Khoản thanh toán này đã bị từ chối.", 409);

        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        await RecomputeInvoiceAsync(payment.Invoice);

        var tenantUserId = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == payment.Invoice.Contract.TenantId)
            .Select(t => t.UserId).FirstOrDefaultAsync();
        _notifications.Add(
            targetRole: "Tenant",
            type: "payment.confirmed",
            title: "Thanh toán đã được xác nhận",
            message: $"Chủ trọ đã xác nhận khoản {payment.Amount:N0} ₫ cho hóa đơn {payment.Invoice.InvoiceCode}.",
            linkPath: "/myinvoices",
            targetUserId: tenantUserId);

        await _db.SaveChangesAsync();
        return DtoMapper.ToPayment(payment);
    }

    /// <summary>Chủ trọ từ chối khoản người thuê báo (không khớp thực tế) + báo lại cho người thuê.</summary>
    public async Task<PaymentDto> RejectAsync(int id, string? reason)
    {
        var payment = await _db.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Contract)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new AppException("Không tìm thấy khoản thanh toán.", 404);
        if (payment.Status != PaymentStatus.Pending)
            throw new AppException("Chỉ từ chối được khoản đang chờ xác nhận.", 409);

        payment.Status = PaymentStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(reason))
            payment.Note = string.IsNullOrWhiteSpace(payment.Note)
                ? $"Từ chối: {reason.Trim()}"
                : $"{payment.Note} | Từ chối: {reason.Trim()}";
        await _db.SaveChangesAsync();

        await RecomputeInvoiceAsync(payment.Invoice);

        var tenantUserId = await _db.Tenants.AsNoTracking()
            .Where(t => t.Id == payment.Invoice.Contract.TenantId)
            .Select(t => t.UserId).FirstOrDefaultAsync();
        _notifications.Add(
            targetRole: "Tenant",
            type: "payment.rejected",
            title: "Thanh toán chưa được xác nhận",
            message: $"Chủ trọ chưa xác nhận khoản {payment.Amount:N0} ₫ cho hóa đơn {payment.Invoice.InvoiceCode}. Vui lòng liên hệ chủ trọ.",
            linkPath: "/myinvoices",
            targetUserId: tenantUserId);

        await _db.SaveChangesAsync();
        return DtoMapper.ToPayment(payment);
    }

    /// <summary>Danh sách khoản đang chờ xác nhận (cho chủ trọ).</summary>
    public async Task<IReadOnlyList<PaymentDto>> ListPendingAsync()
    {
        var list = await _db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
            .ToListAsync();
        return list.Select(DtoMapper.ToPayment).ToList();
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

    /// <summary>Tính lại số đã thu + trạng thái hóa đơn từ các khoản **đã xác nhận**.</summary>
    private async Task RecomputeInvoiceAsync(Invoice invoice)
    {
        var paid = Round(await _db.Payments
            .Where(p => p.InvoiceId == invoice.Id && p.Status == PaymentStatus.Confirmed)
            .SumAsync(p => p.Amount));

        invoice.PaidAmount = paid;
        if (invoice.Status != InvoiceStatus.Cancelled)
        {
            invoice.Status = paid >= invoice.TotalAmount - 0.01m
                ? InvoiceStatus.Paid
                : (paid > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid);
        }
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
