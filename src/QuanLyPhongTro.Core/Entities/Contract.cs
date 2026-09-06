using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Core.Entities;

/// <summary>Hợp đồng thuê phòng giữa chủ trọ và người thuê.</summary>
public class Contract
{
    public int Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal Deposit { get; set; }
    /// <summary>Giá điện (VNĐ/kWh).</summary>
    public decimal ElectricPrice { get; set; }
    /// <summary>Giá nước (VNĐ/m³).</summary>
    public decimal WaterPrice { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public DateTime? TerminatedAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
