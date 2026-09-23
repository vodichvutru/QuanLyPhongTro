using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Core.Entities;

/// <summary>Hóa đơn tiền thuê theo tháng của một hợp đồng/phòng (kèm chỉ số điện/nước đã chốt).</summary>
public class Invoice
{
    public int Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;

    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    /// <summary>Kỳ thanh toán, VD: "2026-08".</summary>
    public string BillingMonth { get; set; } = string.Empty;

    /// <summary>Chỉ số điện đầu kỳ / cuối kỳ chốt tại thời điểm lập hóa đơn (kWh).</summary>
    public decimal ElectricOldIndex { get; set; }
    public decimal ElectricNewIndex { get; set; }
    /// <summary>Chỉ số nước đầu kỳ / cuối kỳ chốt tại thời điểm lập hóa đơn (m³).</summary>
    public decimal WaterOldIndex { get; set; }
    public decimal WaterNewIndex { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PreviousDebt { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
