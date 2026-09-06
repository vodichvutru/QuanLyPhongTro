using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Security;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Tests;

/// <summary>Cấu hình AppDbContext chạy trên EF InMemory cho unit test service.</summary>
public static class TestDb
{
    public static AppDbContext New()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}

/// <summary>Stub phát hành token — service chỉ quan tâm đến hợp đồng interface.</summary>
public class FakeTokenService : ITokenService
{
    public AccessTokenResult CreateAccessToken(int userId, string username, IList<string> roles)
        => new("fake-access-token", DateTime.UtcNow.AddHours(1));

    public string CreateRefreshToken(int userId) => "fake-refresh-token";

    public bool TryGetUserIdFromRefreshToken(string refreshToken, out int userId)
    {
        userId = 1;
        return refreshToken == "valid-refresh";
    }
}
