using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Core.Entities;

/// <summary>Phiếu ghi nhận thanh toán tiền thuê cho một hóa đơn.</summary>
public class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    /// <summary>Trạng thái xác nhận. Thanh toán do người thuê báo = Pending (chờ chủ trọ xác nhận);
    /// chủ trọ tự ghi nhận = Confirmed.</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Confirmed;
    public DateTime? ConfirmedAt { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.Now;
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
