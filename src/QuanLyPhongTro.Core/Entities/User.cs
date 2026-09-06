namespace QuanLyPhongTro.Core.Entities;

/// <summary>Tài khoản đăng nhập hệ thống.</summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
