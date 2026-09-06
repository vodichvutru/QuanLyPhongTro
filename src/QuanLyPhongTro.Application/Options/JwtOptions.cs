namespace QuanLyPhongTro.Application.Options;

/// <summary>Cấu hình JWT từ section "Jwt" trong appsettings.</summary>
public class JwtOptions
{
    public const string Section = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    /// <summary>Thời hạn access token (phút). Mặc định 24h = 1440.</summary>
    public int AccessTokenExpiryMinutes { get; set; } = 1440;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
