namespace QuanLyPhongTro.Core.Entities;

/// <summary>
/// Thông báo trong hệ thống. Gửi cho một vai trò (Owner/Tenant/Admin) khi có sự kiện
/// (người thuê báo thanh toán, gửi yêu cầu sửa chữa, chủ trọ xác nhận thanh toán...).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>Loại sự kiện: payment.pending / payment.confirmed / payment.rejected / repair.created ...</summary>
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>Đường dẫn trong SPA cần mở khi bấm vào thông báo (VD: /invoices).</summary>
    public string? LinkPath { get; set; }

    /// <summary>Vai trò nhận thông báo: Owner / Tenant / Admin.</summary>
    public string TargetRole { get; set; } = "Owner";
    /// <summary>Nếu đặt: chỉ tài khoản này nhận (thông báo riêng của một người thuê). Null = mọi tài khoản thuộc TargetRole.</summary>
    public int? TargetUserId { get; set; }
    /// <summary>Người tạo ra sự kiện (để hiển thị), có thể null.</summary>
    public int? ActorUserId { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
