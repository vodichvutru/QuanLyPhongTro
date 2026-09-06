using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

public class RoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<RoomDto>> ListAsync(RoomStatus? status = null)
    {
        var q = _db.Rooms.AsNoTracking().AsQueryable();
        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);
        var rooms = await q.OrderBy(r => r.Name).ToListAsync();
        return rooms.Select(DtoMapper.ToRoom).ToList();
    }

    public async Task<RoomDto> GetAsync(int id)
    {
        var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new AppException("Không tìm thấy phòng.", 404);
        return DtoMapper.ToRoom(room);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomRequest request)
    {
        Validate(request.Name, request.Price, request.Area);
        var name = request.Name.Trim();
        if (await _db.Rooms.AnyAsync(r => r.Name == name))
            throw new AppException($"Phòng '{name}' đã tồn tại.", 409);

        var room = new Room
        {
            Name = name,
            Floor = request.Floor?.Trim(),
            Area = request.Area,
            Price = request.Price,
            MaxPeople = request.MaxPeople,
            Note = request.Note?.Trim(),
            Status = RoomStatus.Available
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return DtoMapper.ToRoom(room);
    }

    public async Task<RoomDto> UpdateAsync(int id, UpdateRoomRequest request)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new AppException("Không tìm thấy phòng.", 404);

        Validate(request.Name, request.Price, request.Area);
        var name = request.Name.Trim();
        if (await _db.Rooms.AnyAsync(r => r.Name == name && r.Id != id))
            throw new AppException($"Phòng '{name}' đã tồn tại.", 409);

        room.Name = name;
        room.Floor = request.Floor?.Trim();
        room.Area = request.Area;
        room.Price = request.Price;
        room.MaxPeople = request.MaxPeople;
        room.Note = request.Note?.Trim();
        if (request.Status.HasValue)
            room.Status = request.Status.Value;

        await _db.SaveChangesAsync();
        return DtoMapper.ToRoom(room);
    }

    public async Task DeleteAsync(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.Contracts)
            .Include(r => r.MeterReadings)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new AppException("Không tìm thấy phòng.", 404);

        if (room.Contracts.Any(c => c.Status == ContractStatus.Active))
            throw new AppException("Không thể xóa phòng đang có hợp đồng hoạt động.", 409);
        if (room.Contracts.Any() || room.MeterReadings.Any())
            throw new AppException("Không thể xóa phòng đã phát sinh dữ liệu (hợp đồng/chỉ số).", 409);

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
    }

    private static void Validate(string name, decimal price, decimal area)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new AppException("Tên phòng là bắt buộc.");
        if (price <= 0)
            throw new AppException("Giá phòng phải lớn hơn 0.");
        if (area <= 0)
            throw new AppException("Diện tích phòng phải lớn hơn 0.");
    }
}
