using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Core.Entities;

/// <summary>Phòng trọ.</summary>
public class Room
{
    public int Id { get; set; }
    /// <summary>Mã phòng, VD: "P101".</summary>
    public string Name { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public decimal Area { get; set; }
    public decimal Price { get; set; }
    public int MaxPeople { get; set; } = 2;
    public string? Note { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
}
