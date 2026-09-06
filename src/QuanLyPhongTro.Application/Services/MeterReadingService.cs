using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

public class MeterReadingService
{
    private readonly AppDbContext _db;

    public MeterReadingService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MeterReadingDto>> ListByRoomAsync(int roomId)
    {
        if (!await _db.Rooms.AnyAsync(r => r.Id == roomId))
            throw new AppException("Không tìm thấy phòng.", 404);

        var readings = await _db.MeterReadings.AsNoTracking()
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.ReadingDate)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
        var roomName = await _db.Rooms.AsNoTracking().Where(r => r.Id == roomId).Select(r => r.Name).SingleAsync();
        return readings.Select(m => DtoMapper.ToMeterReading(m, roomName)).ToList();
    }

    public async Task<MeterReadingDto> CreateAsync(CreateMeterReadingRequest request)
    {
        if (!await _db.Rooms.AnyAsync(r => r.Id == request.RoomId))
            throw new AppException("Không tìm thấy phòng.", 404);
        if (request.ElectricIndex < 0 || request.WaterIndex < 0)
            throw new AppException("Chỉ số điện/nước không được âm.");

        var last = await _db.MeterReadings
            .Where(m => m.RoomId == request.RoomId)
            .OrderByDescending(m => m.ReadingDate)
            .ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync();
        if (last is not null)
        {
            if (request.ReadingDate.Date <= last.ReadingDate.Date)
                throw new AppException($"Ngày ghi chỉ số phải sau bản ghi gần nhất ({last.ReadingDate:dd/MM/yyyy}).");
            if (request.ElectricIndex < last.ElectricIndex || request.WaterIndex < last.WaterIndex)
                throw new AppException("Chỉ số mới không được nhỏ hơn chỉ số gần nhất đã ghi.");
        }

        var reading = new MeterReading
        {
            RoomId = request.RoomId,
            ReadingDate = request.ReadingDate.Date,
            ElectricIndex = request.ElectricIndex,
            WaterIndex = request.WaterIndex,
            Note = request.Note?.Trim()
        };
        _db.MeterReadings.Add(reading);
        await _db.SaveChangesAsync();

        var roomName = await _db.Rooms.AsNoTracking().Where(r => r.Id == request.RoomId).Select(r => r.Name).SingleAsync();
        return DtoMapper.ToMeterReading(reading, roomName);
    }

    public async Task DeleteAsync(int id)
    {
        var reading = await _db.MeterReadings.FindAsync(id)
            ?? throw new AppException("Không tìm thấy bản ghi chỉ số.", 404);
        _db.MeterReadings.Remove(reading);
        await _db.SaveChangesAsync();
    }
}
