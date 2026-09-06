using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Tests;

public class RepairRequestServiceTests
{
    private async Task<(AppDbContext db, int tenant1UserId, int tenant1Id, int room1Id, int room2Id)> SeedAsync()
    {
        var db = TestDb.New();
        var user1 = new User { Username = "tenant1", PasswordHash = "x", FullName = "Nguyễn Văn Thuê", IsActive = true };
        var user2 = new User { Username = "tenant2", PasswordHash = "x", FullName = "Người Thuê 2", IsActive = true };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var t1 = new Tenant { FullName = user1.FullName, UserId = user1.Id, IsActive = true };
        var t2 = new Tenant { FullName = user2.FullName, UserId = user2.Id, IsActive = true };
        db.Tenants.AddRange(t1, t2);
        await db.SaveChangesAsync();

        var r1 = new Room { Name = "P101", Price = 2_000_000, Area = 20 };
        var r2 = new Room { Name = "P102", Price = 2_500_000, Area = 22 };
        db.Rooms.AddRange(r1, r2);
        await db.SaveChangesAsync();

        // tenant1 đang thuê P101
        db.Contracts.Add(new Contract
        {
            ContractCode = "HD-1", RoomId = r1.Id, TenantId = t1.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 1, 1),
            MonthlyRent = 2_000_000, Status = ContractStatus.Active
        });
        // tenant2 đang thuê P102
        db.Contracts.Add(new Contract
        {
            ContractCode = "HD-2", RoomId = r2.Id, TenantId = t2.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 1, 1),
            MonthlyRent = 2_500_000, Status = ContractStatus.Active
        });
        await db.SaveChangesAsync();

        return (db, user1.Id, t1.Id, r1.Id, r2.Id);
    }

    [Fact] // BR-08 / US-009 AC-04: chỉ gửi yêu cầu cho phòng đang thuê
    public async Task TenantCreateRequest_ForNotOwnedRoom_Throws403()
    {
        var (db, uid, _, _, otherRoomId) = await SeedAsync();
        var svc = new RepairRequestService(db);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            svc.CreateMineAsync(uid, new CreateMyRepairRequestRequest(otherRoomId, "Hỏng điều hòa")));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task TenantCreateRequest_ForOwnedRoom_Succeeds()
    {
        var (db, uid, tenantId, roomId, _) = await SeedAsync();
        var svc = new RepairRequestService(db);

        var result = await svc.CreateMineAsync(uid, new CreateMyRepairRequestRequest(roomId, "Bóng đèn hỏng", "Nhấp nháy"));

        Assert.Equal(RepairStatus.Pending, result.Status);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("Bóng đèn hỏng", result.Subject);
    }

    [Fact]
    public async Task OwnerUpdateStatus_SetsStatusAndCost()
    {
        var (db, uid, _, roomId, _) = await SeedAsync();
        var svc = new RepairRequestService(db);
        var created = await svc.CreateMineAsync(uid, new CreateMyRepairRequestRequest(roomId, "Vỡ bồn cầu"));

        var updated = await svc.UpdateStatusAsync(created.Id, 2,
            new UpdateRepairStatusRequest(RepairStatus.InProgress, "Đã gọi thợ", 200_000));

        Assert.Equal(RepairStatus.InProgress, updated.Status);
        Assert.Equal(200_000, updated.Cost);
    }

    [Fact]
    public async Task OwnerUpdateStatus_AfterCompleted_Throws409()
    {
        var (db, uid, _, roomId, _) = await SeedAsync();
        var svc = new RepairRequestService(db);
        var created = await svc.CreateMineAsync(uid, new CreateMyRepairRequestRequest(roomId, "Hỏng cửa"));
        await svc.UpdateStatusAsync(created.Id, 2, new UpdateRepairStatusRequest(RepairStatus.Completed));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            svc.UpdateStatusAsync(created.Id, 2, new UpdateRepairStatusRequest(RepairStatus.Cancelled)));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task TenantCancel_NonPending_Throws409()
    {
        var (db, uid, _, roomId, _) = await SeedAsync();
        var svc = new RepairRequestService(db);
        var created = await svc.CreateMineAsync(uid, new CreateMyRepairRequestRequest(roomId, "Hỏng đèn"));
        await svc.UpdateStatusAsync(created.Id, 2, new UpdateRepairStatusRequest(RepairStatus.Approved));

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CancelMineAsync(uid, created.Id));
        Assert.Equal(409, ex.StatusCode);
    }
}
