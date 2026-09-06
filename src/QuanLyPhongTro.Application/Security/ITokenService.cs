namespace QuanLyPhongTro.Application.Security;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

/// <summary>Phát hành / xác thực JWT. Implementation nằm ở tầng API (cần package JwtBearer).</summary>
public interface ITokenService
{
    /// <summary>Tạo access token chứa userId, username, roles.</summary>
    AccessTokenResult CreateAccessToken(int userId, string username, IList<string> roles);

    /// <summary>Tạo refresh token dài hạn (loại = refresh).</summary>
    string CreateRefreshToken(int userId);

    /// <summary>Xác thực refresh token, trả về userId nếu hợp lệ.</summary>
    bool TryGetUserIdFromRefreshToken(string refreshToken, out int userId);
}
