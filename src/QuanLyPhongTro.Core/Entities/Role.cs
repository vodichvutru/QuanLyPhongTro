namespace QuanLyPhongTro.Core.Entities;

/// <summary>Vai trò người dùng: Admin, Owner, Tenant.</summary>
public class Role
{
    public int Id { get; set; }
    /// <summary>Mã vai trò: Admin / Owner / Tenant.</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
