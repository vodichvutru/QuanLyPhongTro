using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

public class TenantService
{
    private readonly AppDbContext _db;

    public TenantService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TenantDto>> ListAsync(bool? isActive = null)
    {
        var q = _db.Tenants.AsNoTracking().AsQueryable();
        if (isActive.HasValue)
            q = q.Where(t => t.IsActive == isActive.Value);
        var tenants = await q.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return tenants.Select(DtoMapper.ToTenant).ToList();
    }

    public async Task<TenantDto> GetAsync(int id)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new AppException("Không tìm thấy người thuê.", 404);
        return DtoMapper.ToTenant(tenant);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppException("Họ tên người thuê là bắt buộc.");

        var tenant = new Tenant
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            IdentityNumber = request.IdentityNumber?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            Note = request.Note?.Trim(),
            IsActive = true
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return DtoMapper.ToTenant(tenant);
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantRequest request)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new AppException("Không tìm thấy người thuê.", 404);
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppException("Họ tên người thuê là bắt buộc.");

        tenant.FullName = request.FullName.Trim();
        tenant.Phone = request.Phone?.Trim();
        tenant.IdentityNumber = request.IdentityNumber?.Trim();
        tenant.Email = request.Email?.Trim();
        tenant.Address = request.Address?.Trim();
        tenant.Note = request.Note?.Trim();
        tenant.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return DtoMapper.ToTenant(tenant);
    }

    public async Task DeleteAsync(int id)
    {
        var tenant = await _db.Tenants.Include(t => t.Contracts).FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new AppException("Không tìm thấy người thuê.", 404);
        if (tenant.Contracts.Any(c => c.Status == Core.Enums.ContractStatus.Active))
            throw new AppException("Không thể xóa người thuê đang có hợp đồng hoạt động.", 409);
        if (tenant.Contracts.Any())
            throw new AppException("Không thể xóa người thuê đã có hợp đồng trong quá khứ.", 409);

        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync();
    }
}
