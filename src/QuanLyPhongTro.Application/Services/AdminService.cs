using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

/// <summary>Quản lý User/Role (RBAC) — chỉ Admin sử dụng.</summary>
public class AdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserDto>> ListUsersAsync()
    {
        var users = await _db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Id)
            .ToListAsync();
        return users.Select(DtoMapper.ToUser).ToList();
    }

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync()
    {
        var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
        return roles.Select(r => new RoleDto(r.Id, r.Code, r.Name, r.Description)).ToList();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
            throw new AppException("Tên đăng nhập và mật khẩu là bắt buộc.");
        if (request.Password.Length < 6)
            throw new AppException("Mật khẩu phải có ít nhất 6 ký tự.");
        if (await _db.Users.AnyAsync(u => u.Username == username))
            throw new AppException($"Tên đăng nhập '{username}' đã tồn tại.", 409);

        var roleCodes = request.Roles is { Count: > 0 } ? request.Roles.Distinct().ToList() : new List<string> { "Owner" };
        if (roleCodes.Contains("Tenant"))
            throw new AppException("Tài khoản người thuê phải được tạo từ hồ sơ người thuê (trang Người thuê).");
        var roles = await LoadRolesAsync(roleCodes);

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = true
        };
        foreach (var role in roles)
            user.UserRoles.Add(new UserRole { Role = role });

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return DtoMapper.ToUser(user);
    }

    public async Task<UserDto> UpdateUserRolesAsync(int userId, UpdateUserRolesRequest request)
    {
        var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new AppException("Không tìm thấy người dùng.", 404);

        var roleCodes = request.Roles.Distinct().ToList();
        if (roleCodes.Contains("Tenant") && !await _db.Tenants.AnyAsync(t => t.UserId == userId))
            throw new AppException(
                "Không thể gán vai trò Người thuê cho tài khoản chưa liên kết hồ sơ người thuê. "
                + "Hãy tạo tài khoản người thuê từ hồ sơ (trang Người thuê).", 409);

        user.UserRoles.Clear();
        var roles = await LoadRolesAsync(roleCodes);
        foreach (var role in roles)
            user.UserRoles.Add(new UserRole { Role = role });

        await _db.SaveChangesAsync();
        return DtoMapper.ToUser(user);
    }

    public async Task<UserDto> SetUserActiveAsync(int userId, SetUserActiveRequest request)
    {
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new AppException("Không tìm thấy người dùng.", 404);

        user.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return DtoMapper.ToUser(user);
    }

    /// <summary>Đặt lại mật khẩu cho bất kỳ tài khoản nào (chỉ Admin).</summary>
    public async Task ResetPasswordAsync(int userId, ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new AppException("Không tìm thấy người dùng.", 404);
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new AppException("Mật khẩu mới là bắt buộc.");
        if (request.NewPassword.Length < 6)
            throw new AppException("Mật khẩu phải có ít nhất 6 ký tự.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
    }

    private async Task<List<Role>> LoadRolesAsync(List<string> roleCodes)
    {
        if (roleCodes.Count == 0)
            return new List<Role>();

        var roles = await _db.Roles.Where(r => roleCodes.Contains(r.Code)).ToListAsync();
        var missing = roleCodes.Except(roles.Select(r => r.Code)).ToList();
        if (missing.Count > 0)
            throw new AppException($"Vai trò không tồn tại: {string.Join(", ", missing)}.");
        return roles;
    }
}
