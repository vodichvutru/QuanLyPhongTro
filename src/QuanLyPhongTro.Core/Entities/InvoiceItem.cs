namespace QuanLyPhongTro.Core.Entities;

/// <summary>Dòng chi tiết trong hóa đơn (tiền phòng, điện, nước, dịch vụ...).</summary>
public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
