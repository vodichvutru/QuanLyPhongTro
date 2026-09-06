using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Entities;

namespace QuanLyPhongTro.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRoles()
    {
        await using var db = TestDb.New();
        var role = new Role { Code = "Owner", Name = "Chủ trọ" };
        var user = new User
        {
            Username = "chutro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Trần Phong",
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { Role = role });
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new AuthService(db, new FakeTokenService());
        var result = await svc.LoginAsync(new LoginRequest("chutro", "123456"));

        Assert.Equal(user.Id, result.User.Id);
        Assert.Contains("Owner", result.User.Roles);
        Assert.Equal("fake-access-token", result.AccessToken);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws401()
    {
        await using var db = TestDb.New();
        db.Users.Add(new User
        {
            Username = "chutro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "A",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var svc = new AuthService(db, new FakeTokenService());
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.LoginAsync(new LoginRequest("chutro", "sai-mat-khau")));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task Login_InactiveUser_Throws401()
    {
        await using var db = TestDb.New();
        db.Users.Add(new User
        {
            Username = "khoa",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "A",
            IsActive = false
        });
        await db.SaveChangesAsync();

        var svc = new AuthService(db, new FakeTokenService());
        await Assert.ThrowsAsync<AppException>(() => svc.LoginAsync(new LoginRequest("khoa", "123456")));
    }
}
