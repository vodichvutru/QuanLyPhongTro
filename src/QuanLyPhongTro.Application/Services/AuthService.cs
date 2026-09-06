using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Security;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

/// <summary>Đăng nhập / refresh token / lấy thông tin tài khoản hiện tại.</summary>
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;

    public AuthService(AppDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await QueryUserAsync(request.Username.Trim());
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException("Tên đăng nhập hoặc mật khẩu không đúng.", 401);
        }

        return BuildTokenResponse(user);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshRequest request)
    {
        if (!_tokens.TryGetUserIdFromRefreshToken(request.RefreshToken, out var userId))
        {
            throw new AppException("Refresh token không hợp lệ hoặc đã hết hạn.", 401);
        }

        var user = await QueryUserByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            throw new AppException("Tài khoản không tồn tại hoặc đã bị khóa.", 401);
        }

        return BuildTokenResponse(user);
    }

    public async Task<UserDto> GetMeAsync(int userId)
    {
        var user = await QueryUserByIdAsync(userId)
            ?? throw new AppException("Không tìm thấy người dùng.", 404);
        return DtoMapper.ToUser(user);
    }

    private TokenResponse BuildTokenResponse(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var access = _tokens.CreateAccessToken(user.Id, user.Username, roles);
        var refresh = _tokens.CreateRefreshToken(user.Id);

        return new TokenResponse(access.Token, refresh, access.ExpiresAtUtc, DtoMapper.ToUser(user));
    }

    private Task<User?> QueryUserAsync(string username) =>
        _db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

    private Task<User?> QueryUserByIdAsync(int id) =>
        _db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
}
