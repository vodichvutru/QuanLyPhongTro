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

        var createAccount = !string.IsNullOrWhiteSpace(request.Username);
        if (!createAccount && !string.IsNullOrWhiteSpace(request.Password))
            throw new AppException("Để tạo tài khoản đăng nhập bạn phải nhập tên đăng nhập.");

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
        if (createAccount)
        {
            var (username, password) = await ValidateNewAccountAsync(request.Username!, request.Password!);
            tenant.User = await CreateTenantUserAsync(username, password, tenant.FullName);
        }

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return DtoMapper.ToTenant(tenant);
    }

    /// <summary>Tạo tài khoản đăng nhập (role Tenant) cho một hồ sơ người thuê chưa có tài khoản.</summary>
    public async Task<TenantDto> CreateAccountAsync(int tenantId, CreateTenantAccountRequest request)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new AppException("Không tìm thấy người thuê.", 404);
        if (tenant.UserId.HasValue)
            throw new AppException("Hồ sơ người thuê này đã có tài khoản đăng nhập.", 409);

        var (username, password) = await ValidateNewAccountAsync(request.Username, request.Password);
        tenant.User = await CreateTenantUserAsync(username, password, tenant.FullName);
        await _db.SaveChangesAsync();
        return DtoMapper.ToTenant(tenant);
    }

    /// <summary>Đặt lại mật khẩu cho tài khoản của một hồ sơ người thuê (Admin/Chủ trọ thao tác hộ).</summary>
    public async Task ResetPasswordAsync(int tenantId, ResetPasswordRequest request)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId)
            ?? throw new AppException("Không tìm thấy người thuê.", 404);
        if (!tenant.UserId.HasValue)
            throw new AppException("Hồ sơ người thuê này chưa có tài khoản đăng nhập.", 409);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == tenant.UserId)
            ?? throw new AppException("Không tìm thấy tài khoản người thuê.", 404);

        ValidatePassword(request.NewPassword);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
    }

    // ---- Account helpers (role Tenant, bắt buộc gắn với hồ sơ) ----

    private async Task<(string Username, string Password)> ValidateNewAccountAsync(string username, string? password)
    {
        ValidatePassword(password);
        username = username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            throw new AppException("Tên đăng nhập là bắt buộc.");
        if (await _db.Users.AnyAsync(u => u.Username == username))
            throw new AppException($"Tên đăng nhập '{username}' đã tồn tại.", 409);
        return (username, password!);
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new AppException("Mật khẩu là bắt buộc.");
        if (password!.Length < 6)
            throw new AppException("Mật khẩu phải có ít nhất 6 ký tự.");
    }

    private async Task<User> CreateTenantUserAsync(string username, string password, string fullName)
    {
        var role = await _db.Roles.SingleAsync(r => r.Code == "Tenant");
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { Role = role });
        _db.Users.Add(user);
        return user;
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
