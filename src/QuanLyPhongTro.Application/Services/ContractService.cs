using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

public class ContractService
{
    private readonly AppDbContext _db;

    public ContractService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContractDto>> ListAsync(int? roomId = null, int? tenantId = null)
    {
        await MarkExpiredAsync();
        var q = _db.Contracts.AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .AsQueryable();
        if (roomId.HasValue) q = q.Where(c => c.RoomId == roomId);
        if (tenantId.HasValue) q = q.Where(c => c.TenantId == tenantId);

        var list = await q.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return list.Select(DtoMapper.ToContract).ToList();
    }

    public async Task<ContractDto> GetAsync(int id)
    {
        await MarkExpiredAsync();
        var contract = await _db.Contracts.AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new AppException("Không tìm thấy hợp đồng.", 404);
        return DtoMapper.ToContract(contract);
    }

    public async Task<ContractDto> CreateAsync(CreateContractRequest request)
    {
        if (request.MonthlyRent <= 0)
            throw new AppException("Tiền thuê hàng tháng phải lớn hơn 0.");
        if (request.EndDate.HasValue && request.EndDate.Value.Date <= request.StartDate.Date)
            throw new AppException("Ngày kết thúc phải sau ngày bắt đầu.");
        if (request.EndDate.HasValue && request.EndDate.Value.Date < DateTime.Today)
            throw new AppException("Ngày kết thúc không được ở quá khứ.");
        if (request.ElectricPrice <= 0 || request.WaterPrice <= 0)
            throw new AppException("Giá điện và giá nước phải lớn hơn 0.");

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId)
            ?? throw new AppException("Không tìm thấy phòng.", 404);
        if (!await _db.Tenants.AnyAsync(t => t.Id == request.TenantId))
            throw new AppException("Không tìm thấy người thuê.", 404);

        // Phòng chỉ có 1 hợp đồng còn hiệu lực tại một thời điểm
        var conflict = await _db.Contracts.AnyAsync(c =>
            c.RoomId == request.RoomId && c.Status == ContractStatus.Active &&
            (request.EndDate == null || (c.StartDate <= request.EndDate.Value && (c.EndDate == null || c.EndDate.Value >= request.StartDate))));
        if (conflict)
            throw new AppException("Phòng này đang có hợp đồng hoạt động trong khoảng thời gian trên.", 409);

        var code = $"HD-{request.StartDate:yyyyMMdd}-{await _db.Contracts.CountAsync() + 1:000}";
        var contract = new Contract
        {
            ContractCode = code,
            RoomId = request.RoomId,
            TenantId = request.TenantId,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate?.Date,
            MonthlyRent = request.MonthlyRent,
            Deposit = request.Deposit,
            ElectricPrice = request.ElectricPrice,
            WaterPrice = request.WaterPrice,
            Note = request.Note?.Trim(),
            Status = ContractStatus.Active
        };
        _db.Contracts.Add(contract);
        room.Status = RoomStatus.Rented;
        await _db.SaveChangesAsync();

        return await GetAsync(contract.Id);
    }

    public async Task<ContractDto> TerminateAsync(int id, TerminateContractRequest request)
    {
        await MarkExpiredAsync();
        var contract = await _db.Contracts
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new AppException("Không tìm thấy hợp đồng.", 404);
        if (contract.Status == ContractStatus.Terminated)
            throw new AppException("Hợp đồng đã được chấm dứt trước đó.", 409);
        if (contract.Status == ContractStatus.Expired)
            throw new AppException("Hợp đồng đã hết hạn (không cần chấm dứt).", 409);

        contract.Status = ContractStatus.Terminated;
        contract.TerminatedAt = request.TerminatedAt ?? DateTime.Now;
        if (!string.IsNullOrWhiteSpace(request.Reason))
            contract.Note = string.IsNullOrEmpty(contract.Note)
                ? $"Lý do chấm dứt: {request.Reason.Trim()}"
                : $"{contract.Note} | Lý do chấm dứt: {request.Reason.Trim()}";

        // Cập nhật trạng thái phòng nếu không còn hợp đồng hoạt động
        var hasOtherActive = await _db.Contracts.AnyAsync(c =>
            c.RoomId == contract.RoomId && c.Id != contract.Id && c.Status == ContractStatus.Active);
        if (!hasOtherActive)
            contract.Room.Status = RoomStatus.Available;

        await _db.SaveChangesAsync();
        return DtoMapper.ToContract(contract);
    }

    /// <summary>Tự động đánh dấu hợp đồng hết hạn khi quá ngày kết thúc.</summary>
    private async Task MarkExpiredAsync()
    {
        var today = DateTime.Today;
        var expired = await _db.Contracts
            .Include(c => c.Room)
            .Where(c => c.Status == ContractStatus.Active && c.EndDate != null && c.EndDate.Value.Date < today)
            .ToListAsync();
        if (expired.Count == 0)
            return;

        foreach (var c in expired)
        {
            c.Status = ContractStatus.Expired;
            var stillActive = await _db.Contracts.AnyAsync(x =>
                x.RoomId == c.RoomId && x.Id != c.Id && x.Status == ContractStatus.Active);
            if (!stillActive)
                c.Room.Status = RoomStatus.Available;
        }
        await _db.SaveChangesAsync();
    }
}
