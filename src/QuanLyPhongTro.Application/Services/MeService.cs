using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

/// <summary>
/// Cổng tự phục vụ của người thuê (Tenant Portal): chỉ xem được dữ liệu của chính mình
/// qua liên kết Tenant.UserId == tài khoản đang đăng nhập.
/// </summary>
public class MeService
{
    private readonly AppDbContext _db;

    public MeService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContractDto>> GetMyContractsAsync(int userId)
    {
        var tenantId = await GetTenantIdAsync(userId);
        var contracts = await _db.Contracts.AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
        return contracts.Select(DtoMapper.ToContract).ToList();
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetMyInvoicesAsync(int userId)
    {
        var tenantId = await GetTenantIdAsync(userId);
        var invoices = await _db.Invoices.AsNoTracking()
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .Include(i => i.Contract).ThenInclude(c => c.Tenant)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.Contract.TenantId == tenantId && i.Status != Core.Enums.InvoiceStatus.Cancelled)
            .OrderByDescending(i => i.BillingMonth).ThenByDescending(i => i.Id)
            .ToListAsync();
        return invoices.Select(DtoMapper.ToInvoice).ToList();
    }

    public async Task<InvoiceDto> GetMyInvoiceAsync(int userId, int invoiceId)
    {
        var tenantId = await GetTenantIdAsync(userId);
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .Include(i => i.Contract).ThenInclude(c => c.Tenant)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Contract.TenantId == tenantId && i.Status != Core.Enums.InvoiceStatus.Cancelled)
            ?? throw new AppException("Không tìm thấy hóa đơn.", 404);
        return DtoMapper.ToInvoice(invoice);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetMyPaymentsAsync(int userId)
    {
        var tenantId = await GetTenantIdAsync(userId);
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.Invoice.Contract.TenantId == tenantId)
            .OrderByDescending(p => p.PaidAt).ThenByDescending(p => p.Id)
            .ToListAsync();
        return payments.Select(DtoMapper.ToPayment).ToList();
    }

    private async Task<int> GetTenantIdAsync(int userId)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive);
        return tenant?.Id
            ?? throw new AppException("Tài khoản của bạn chưa được liên kết với hồ sơ người thuê. Liên hệ chủ trọ.", 403);
    }
}
