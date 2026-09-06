namespace QuanLyPhongTro.Core.Entities;

/// <summary>Bản ghi chỉ số đồng hồ điện/nước của một phòng tại một thời điểm.</summary>
public class MeterReading
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTime ReadingDate { get; set; }
    /// <summary>Chỉ số điện (kWh).</summary>
    public decimal ElectricIndex { get; set; }
    /// <summary>Chỉ số nước (m³).</summary>
    public decimal WaterIndex { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
