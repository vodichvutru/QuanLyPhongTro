namespace QuanLyPhongTro.Core.Entities;

/// <summary>Người thuê trọ. Có thể liên kết tài khoản (UserId) để tự tra cứu.</summary>
public class Tenant
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    /// <summary>Căn cước công dân / CMND.</summary>
    public string? IdentityNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Liên kết tới tài khoản đăng nhập (nếu có) để dùng cổng tra cứu.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
