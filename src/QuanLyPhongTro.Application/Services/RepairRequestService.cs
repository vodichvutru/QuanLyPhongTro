using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

public class RepairRequestService
{
    private readonly AppDbContext _db;

    public RepairRequestService(AppDbContext db) => _db = db;

    // ---------- Chủ trọ / Admin ----------

    public async Task<IReadOnlyList<RepairRequestDto>> ListAsync(RepairStatus? status = null, int? roomId = null)
    {
        var q = _db.RepairRequests.AsNoTracking()
            .Include(r => r.Room)
            .Include(r => r.Tenant)
            .AsQueryable();
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        if (roomId.HasValue) q = q.Where(r => r.RoomId == roomId.Value);

        var list = await q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).ToListAsync();
        return list.Select(DtoMapper.ToRepair).ToList();
    }

    public async Task<RepairRequestDto> GetAsync(int id)
    {
        var request = await QueryDetailedAsync(id)
            ?? throw new AppException("Không tìm thấy yêu cầu sửa chữa.", 404);
        return DtoMapper.ToRepair(request);
    }

    /// <summary>Chủ trọ tạo yêu cầu thay người thuê (hoặc cho sẵn người thuê đang ở phòng đó).</summary>
    public async Task<RepairRequestDto> CreateAsync(int byUserId, CreateRepairRequestRequest request)
    {
        if (!await _db.Rooms.AnyAsync(r => r.Id == request.RoomId))
            throw new AppException("Không tìm thấy phòng.", 404);
        Validate(request.Subject, request.Description);

        int? tenantId = request.TenantId;
        if (!tenantId.HasValue)
        {
            // Tự gán người thuê đang có hợp đồng hoạt động với phòng
            tenantId = await _db.Contracts.AsNoTracking()
                .Where(c => c.RoomId == request.RoomId && c.Status == ContractStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .Select(c => (int?)c.TenantId)
                .FirstOrDefaultAsync();
        }
        else if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId))
        {
            throw new AppException("Không tìm thấy người thuê.", 404);
        }

        var byName = await GetUserNameAsync(byUserId);
        var repair = new RepairRequest
        {
            RoomId = request.RoomId,
            TenantId = tenantId,
            Subject = request.Subject.Trim(),
            Description = request.Description?.Trim(),
            Status = RepairStatus.Pending,
            CreatedByUserId = byUserId,
            CreatedByName = byName
        };
        _db.RepairRequests.Add(repair);
        await _db.SaveChangesAsync();
        return await GetAsync(repair.Id);
    }

    /// <summary>Chủ trọ cập nhật trạng thái + phản hồi/chi phí.</summary>
    public async Task<RepairRequestDto> UpdateStatusAsync(int id, int byUserId, UpdateRepairStatusRequest request)
    {
        var repair = await _db.RepairRequests.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new AppException("Không tìm thấy yêu cầu sửa chữa.", 404);
        if (repair.Status == RepairStatus.Completed || repair.Status == RepairStatus.Cancelled)
            throw new AppException("Yêu cầu đã kết thúc, không thể thay đổi trạng thái.", 409);
        if (request.Cost.HasValue && request.Cost.Value < 0)
            throw new AppException("Chi phí không được âm.");

        repair.Status = request.Status;
        if (request.OwnerNote is not null)
            repair.OwnerNote = request.OwnerNote.Trim();
        if (request.Cost.HasValue)
            repair.Cost = request.Cost.Value;
        repair.HandledAt = DateTime.Now;
        repair.HandledByName = await GetUserNameAsync(byUserId);

        await _db.SaveChangesAsync();
        return await GetAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var repair = await _db.RepairRequests.FindAsync(id)
            ?? throw new AppException("Không tìm thấy yêu cầu sửa chữa.", 404);
        if (repair.Status is RepairStatus.InProgress or RepairStatus.Completed)
            throw new AppException("Không thể xóa yêu cầu đang/dã xử lý.", 409);

        _db.RepairRequests.Remove(repair);
        await _db.SaveChangesAsync();
    }

    // ---------- Cổng người thuê ----------

    public async Task<IReadOnlyList<RepairRequestDto>> ListMineAsync(int userId)
    {
        var tenantId = await GetTenantIdAsync(userId);
        var list = await _db.RepairRequests.AsNoTracking()
            .Include(r => r.Room)
            .Include(r => r.Tenant)
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return list.Select(DtoMapper.ToRepair).ToList();
    }

    public async Task<RepairRequestDto> CreateMineAsync(int userId, CreateMyRepairRequestRequest request)
    {
        var tenantId = await GetTenantIdAsync(userId);
        if (!await _db.Rooms.AnyAsync(r => r.Id == request.RoomId))
            throw new AppException("Không tìm thấy phòng.", 404);

        // BR-08: tenant chỉ gửi yêu cầu cho phòng đang thuê (hợp đồng còn hiệu lực)
        var isMyRoom = await _db.Contracts.AsNoTracking().AnyAsync(c =>
            c.TenantId == tenantId && c.RoomId == request.RoomId && c.Status == ContractStatus.Active);
        if (!isMyRoom)
            throw new AppException("Bạn chỉ có thể gửi yêu cầu cho phòng đang thuê (hợp đồng còn hiệu lực).", 403);

        Validate(request.Subject, request.Description);

        var tenantName = await _db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.FullName).SingleAsync();
        var repair = new RepairRequest
        {
            RoomId = request.RoomId,
            TenantId = tenantId,
            Subject = request.Subject.Trim(),
            Description = request.Description?.Trim(),
            Status = RepairStatus.Pending,
            CreatedByUserId = userId,
            CreatedByName = tenantName
        };
        _db.RepairRequests.Add(repair);
        await _db.SaveChangesAsync();
        return await GetAsync(repair.Id);
    }

    public async Task<RepairRequestDto> GetMineAsync(int userId, int id)
    {
        var tenantId = await GetTenantIdAsync(userId);
        var request = await QueryDetailedAsync(id)
            ?? throw new AppException("Không tìm thấy yêu cầu sửa chữa.", 404);
        if (request.TenantId != tenantId)
            throw new AppException("Không tìm thấy yêu cầu sửa chữa.", 404);
        return DtoMapper.ToRepair(request);
    }

    public async Task<RepairRequestDto> CancelMineAsync(int userId, int id)
    {
        var tenantId = await GetTenantIdAsync(userId);
        var repair = await _db.RepairRequests.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new AppException("Không tìm thấy yêu cầu sửa chữa.", 404);
        if (repair.Status != RepairStatus.Pending)
            throw new AppException("Chỉ hủy được yêu cầu đang ở trạng thái chờ xử lý.", 409);

        repair.Status = RepairStatus.Cancelled;
        await _db.SaveChangesAsync();
        return await GetAsync(id);
    }

    // ---------- helpers ----------

    private Task<RepairRequest?> QueryDetailedAsync(int id) =>
        _db.RepairRequests.AsNoTracking()
            .Include(r => r.Room)
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.Id == id);

    private async Task<string> GetUserNameAsync(int userId)
        => await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync()
           ?? $"User#{userId}";

    private async Task<int> GetTenantIdAsync(int userId)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive);
        return tenant?.Id
            ?? throw new AppException("Tài khoản của bạn chưa được liên kết với hồ sơ người thuê. Liên hệ chủ trọ.", 403);
    }

    private static void Validate(string subject, string? description)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new AppException("Tiêu đề yêu cầu là bắt buộc.");
        if (subject.Trim().Length > 150)
            throw new AppException("Tiêu đề không quá 150 ký tự.");
        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 1000)
            throw new AppException("Mô tả không quá 1000 ký tự.");
    }
}
