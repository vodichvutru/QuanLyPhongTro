using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Core.Entities;

/// <summary>Yêu cầu sửa chữa — người thuê gửi, chủ trọ xử lý.</summary>
public class RepairRequest
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    /// <summary>Người thuê gửi yêu cầu (nếu tạo qua cổng tenant).</summary>
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RepairStatus Status { get; set; } = RepairStatus.Pending;

    /// <summary>Phản hồi của chủ trọ.</summary>
    public string? OwnerNote { get; set; }
    /// <summary>Chi phí sửa chữa dự kiến (nếu có).</summary>
    public decimal? Cost { get; set; }

    /// <summary>Id + tên người tạo (chụp nhanh để hiển thị).</summary>
    public int? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Thời điểm chủ trọ cập nhật trạng thái gần nhất.</summary>
    public DateTime? HandledAt { get; set; }
    public string? HandledByName { get; set; }
}
