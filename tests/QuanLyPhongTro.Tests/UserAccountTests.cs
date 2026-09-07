using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Tests;

/// <summary>
/// Luật cấp tài khoản: Admin tạo tài khoản Chủ trọ; Admin & Chủ trọ tạo tài khoản Người thuê
/// gắn với hồ sơ; người thuê không tự đăng ký; mật khẩu quên được đặt lại hộ (Admin/Chủ trọ).
/// </summary>
public class UserAccountTests
{
    private static async Task SeedRolesAsync(AppDbContext db, params string[] codes)
    {
        foreach (var code in codes)
            db.Roles.Add(new Role { Code = code, Name = code });
        await db.SaveChangesAsync();
    }

    private static async Task<User> SeedUserWithRoleAsync(AppDbContext db, string username, string roleCode, string password = "123456")
    {
        var role = await db.Roles.SingleAsync(r => r.Code == roleCode);
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = username,
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { Role = role });
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ---------- TenantService: tài khoản người thuê ----------

    [Fact]
    public async Task CreateAccount_ForTenantWithoutAccount_CreatesLinkedTenantUser()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Tenant");
        var tenant = new Tenant { FullName = "Nguyễn Văn Thuê", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var svc = new TenantService(db);
        var result = await svc.CreateAccountAsync(tenant.Id, new CreateTenantAccountRequest("nguyenvanthe", "abc123"));

        Assert.True(result.HasAccount);
        var user = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleAsync(u => u.Username == "nguyenvanthe");
        Assert.Equal(user.Id, tenant.UserId); // hồ sơ được liên kết tới tài khoản mới
        Assert.Contains("Tenant", user.UserRoles.Select(ur => ur.Role.Code));
        Assert.Equal("Nguyễn Văn Thuê", user.FullName);
    }

    [Fact]
    public async Task CreateAccount_WhenTenantAlreadyHasAccount_Throws409()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Tenant");
        var tenant = new Tenant { FullName = "A", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        tenant.UserId = (await SeedUserWithRoleAsync(db, "a_account", "Tenant")).Id;
        await db.SaveChangesAsync();

        var svc = new TenantService(db);
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateAccountAsync(tenant.Id, new CreateTenantAccountRequest("a_account2", "abc123")));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_DuplicateUsername_Throws409()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Tenant");
        db.Users.Add(new User { Username = "trung", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), FullName = "X", IsActive = true });
        var tenant = new Tenant { FullName = "B", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var svc = new TenantService(db);
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateAccountAsync(tenant.Id, new CreateTenantAccountRequest("trung", "abc123")));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateTenant_WithUsernameAndPassword_CreatesLinkedAccountInOneShot()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Tenant");

        var svc = new TenantService(db);
        var result = await svc.CreateAsync(new CreateTenantRequest(
            "Lê Văn Mới", Username: "levanmoi", Password: "abc123"));

        Assert.True(result.HasAccount);
        Assert.True(await db.Users.AnyAsync(u => u.Username == "levanmoi"));
    }

    [Fact]
    public async Task ResetPassword_ThenLoginWithNewPassword_SucceedsAndOldFails()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Tenant");
        var tenant = new Tenant { FullName = "C", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        tenant.UserId = (await SeedUserWithRoleAsync(db, "c_account", "Tenant", "123456")).Id;
        await db.SaveChangesAsync();

        var svc = new TenantService(db);
        await svc.ResetPasswordAsync(tenant.Id, new ResetPasswordRequest("matkhaumoi"));

        var auth = new AuthService(db, new FakeTokenService());
        var result = await auth.LoginAsync(new LoginRequest("c_account", "matkhaumoi"));
        Assert.Equal("c_account", result.User.Username);
        await Assert.ThrowsAsync<AppException>(() => auth.LoginAsync(new LoginRequest("c_account", "123456")));
    }

    // ---------- AdminService: tài khoản quản trị / chủ trọ + đặt lại mật khẩu ----------

    [Fact]
    public async Task AdminCreateUser_WithTenantRole_IsRejected()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Owner", "Tenant");

        var svc = new AdminService(db);
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateUserAsync(new CreateUserRequest("tenantx", "abc123", "X", Roles: new List<string> { "Tenant" })));

        Assert.Contains("Người thuê", ex.Message);
    }

    [Fact]
    public async Task AdminCreateUser_OwnerRole_CreatesOwnerAccount()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Owner");

        var svc = new AdminService(db);
        var user = await svc.CreateUserAsync(new CreateUserRequest("chutro2", "abc123", "Chủ Trọ Hai", Roles: new List<string> { "Owner" }));

        Assert.Equal("chutro2", user.Username);
        Assert.Contains("Owner", user.Roles);
    }

    [Fact]
    public async Task AdminAssignTenantRole_ToUserWithoutLinkedTenant_Throws409()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Owner", "Tenant");
        var user = await SeedUserWithRoleAsync(db, "staff", "Owner");

        var svc = new AdminService(db);
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.UpdateUserRolesAsync(user.Id, new UpdateUserRolesRequest(new List<string> { "Tenant" })));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task AdminResetPassword_ThenLoginWithNewPassword()
    {
        await using var db = TestDb.New();
        await SeedRolesAsync(db, "Owner");
        var user = await SeedUserWithRoleAsync(db, "owner2", "Owner", "123456");

        var svc = new AdminService(db);
        await svc.ResetPasswordAsync(user.Id, new ResetPasswordRequest("matkhaumoi"));

        var auth = new AuthService(db, new FakeTokenService());
        var result = await auth.LoginAsync(new LoginRequest("owner2", "matkhaumoi"));
        Assert.Equal("Owner", result.User.Roles.FirstOrDefault());
    }
}
