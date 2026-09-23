using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Tests;

/// <summary>
/// Thanh toán trực tuyến của người thuê (ghi nhận ngay, trả đủ) + dự toán/thông tin lập hóa đơn
/// (chỉ số điện/nước được nhập trực tiếp trên hóa đơn).
/// </summary>
public class TenantPayAndInvoicePreviewTests
{
    private sealed record PaySeed(AppDbContext Db, int UserA, int InvoiceAId, int InvoiceBId);

    private static async Task<PaySeed> SeedTwoTenantsWithInvoicesAsync()
    {
        var db = TestDb.New();
        var roomA = new Room { Name = "P101", Price = 2_000_000, Area = 20 };
        var roomB = new Room { Name = "P102", Price = 2_500_000, Area = 22 };
        db.Rooms.AddRange(roomA, roomB);
        await db.SaveChangesAsync();

        var userA = new User { Username = "a", PasswordHash = "x", FullName = "Thuê A", IsActive = true };
        var userB = new User { Username = "b", PasswordHash = "x", FullName = "Thuê B", IsActive = true };
        db.Users.AddRange(userA, userB);
        await db.SaveChangesAsync();

        var tA = new Tenant { FullName = "Thuê A", UserId = userA.Id, IsActive = true };
        var tB = new Tenant { FullName = "Thuê B", UserId = userB.Id, IsActive = true };
        db.Tenants.AddRange(tA, tB);
        await db.SaveChangesAsync();

        var cA = new Contract
        {
            ContractCode = "HD-A", RoomId = roomA.Id, TenantId = tA.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 1, 1),
            MonthlyRent = 2_000_000, ElectricPrice = 3500, WaterPrice = 25000, Status = ContractStatus.Active
        };
        var cB = new Contract
        {
            ContractCode = "HD-B", RoomId = roomB.Id, TenantId = tB.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 1, 1),
            MonthlyRent = 2_500_000, ElectricPrice = 3500, WaterPrice = 25000, Status = ContractStatus.Active
        };
        db.Contracts.AddRange(cA, cB);
        await db.SaveChangesAsync();

        var invA = new Invoice { InvoiceCode = "INV-A", ContractId = cA.Id, BillingMonth = "2026-08", TotalAmount = 2_000_000, Status = InvoiceStatus.Unpaid };
        var invB = new Invoice { InvoiceCode = "INV-B", ContractId = cB.Id, BillingMonth = "2026-08", TotalAmount = 2_500_000, Status = InvoiceStatus.Unpaid };
        db.Invoices.AddRange(invA, invB);
        await db.SaveChangesAsync();

        return new PaySeed(db, userA.Id, invA.Id, invB.Id);
    }

    [Fact]
    public async Task TenantPay_CreatesPendingPayment_InvoiceWaitsForOwnerConfirmation()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db, new NotificationService(seed.Db));

        var payment = await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId,
            new CreateMyPaymentRequest(PaymentMethod.Momo, Reference: "WEB-TEST-1"));

        Assert.Equal(2_000_000m, payment.Amount);
        Assert.StartsWith("WEB-TEST-1", payment.Reference);
        Assert.Equal(PaymentStatus.Pending, payment.Status);

        // Chưa tính vào số đã thu — chờ chủ trọ xác nhận
        var invoice = await seed.Db.Invoices.AsNoTracking().SingleAsync(i => i.Id == seed.InvoiceAId);
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);
        Assert.Equal(0m, invoice.PaidAmount);
        Assert.Equal(1, await seed.Db.Payments.CountAsync(p => p.InvoiceId == seed.InvoiceAId));

        // Có thông báo cho chủ trọ
        Assert.Equal(1, await seed.Db.Notifications.CountAsync(n => n.TargetRole == "Owner" && n.Type == "payment.pending"));
    }

    [Fact]
    public async Task OwnerConfirmsTenantPayment_MarksInvoicePaid_AndNotifiesTenant()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db, new NotificationService(seed.Db));
        var pending = await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.Momo));

        var confirmed = await svc.ConfirmAsync(pending.Id);

        Assert.Equal(PaymentStatus.Confirmed, confirmed.Status);
        var invoice = await seed.Db.Invoices.AsNoTracking().SingleAsync(i => i.Id == seed.InvoiceAId);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(2_000_000m, invoice.PaidAmount);
        Assert.Equal(1, await seed.Db.Notifications.CountAsync(n => n.TargetRole == "Tenant" && n.Type == "payment.confirmed"));
    }

    [Fact]
    public async Task OwnerRejectsTenantPayment_InvoiceStaysUnpaid()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db, new NotificationService(seed.Db));
        var pending = await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.Momo));

        var rejected = await svc.RejectAsync(pending.Id, "Chưa nhận được tiền");

        Assert.Equal(PaymentStatus.Rejected, rejected.Status);
        var invoice = await seed.Db.Invoices.AsNoTracking().SingleAsync(i => i.Id == seed.InvoiceAId);
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);
        Assert.Equal(0m, invoice.PaidAmount);
    }

    [Fact]
    public async Task TenantPay_EmptyReference_IsGeneratedWithWebPrefix()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db, new NotificationService(seed.Db));

        var payment = await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.VnPay));

        Assert.StartsWith("WEB-", payment.Reference);
    }

    [Fact]
    public async Task TenantPay_OtherTenantsInvoice_Throws404()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db, new NotificationService(seed.Db));

        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateByTenantAsync(seed.UserA, seed.InvoiceBId, new CreateMyPaymentRequest(PaymentMethod.Momo)));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task TenantPay_AlreadyPaidInvoice_Throws()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db, new NotificationService(seed.Db));
        await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.Momo));

        await Assert.ThrowsAsync<AppException>(
            () => svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.Momo)));
    }

    [Fact]
    public async Task TenantPay_CashMethod_IsRejected()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db, new NotificationService(seed.Db));

        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.Cash)));

        Assert.Contains("chỉ hỗ trợ", ex.Message);
    }

    [Fact]
    public async Task TenantPay_UserWithoutTenantProfile_Throws403()
    {
        var db = TestDb.New();
        db.Users.Add(new User { Username = "no-link", PasswordHash = "x", FullName = "X", IsActive = true });
        await db.SaveChangesAsync();

        var svc = new PaymentService(db, new NotificationService(db));
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateByTenantAsync(db.Users.First().Id, 1, new CreateMyPaymentRequest(PaymentMethod.Momo)));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task InvoicePreview_ComputesElectricWater_WithoutSaving()
    {
        var db = TestDb.New();
        var room = new Room { Name = "P101", Price = 2_000_000, Area = 20 };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        var tenant = new Tenant { FullName = "Thuê", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var contract = new Contract
        {
            ContractCode = "HD", RoomId = room.Id, TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = null,
            MonthlyRent = 2_000_000, ElectricPrice = 3500, WaterPrice = 25000, Status = ContractStatus.Active
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var svc = new InvoiceService(db);
        // Chỉ số đầu kỳ 100/10 → cuối kỳ 150/13 = dùng 50 kWh và 3 m³
        var preview = await svc.PreviewAsync(new CreateInvoiceRequest(room.Id, "2026-02", 100, 150, 10, 13));

        Assert.Equal("P101", preview.RoomName);
        Assert.Equal(3, preview.Items.Count);
        // Tiền phòng 2.000.000 + điện 50×3500 + nước 3×25000
        Assert.Equal(2_250_000m, preview.TotalAmount);
        Assert.Equal(0, await db.Invoices.CountAsync()); // chưa lưu hóa đơn nào
    }

    [Fact]
    public async Task InvoicePreview_ZeroUsage_OnlyRentItem()
    {
        var db = TestDb.New();
        var room = new Room { Name = "P101", Price = 2_000_000, Area = 20 };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        var tenant = new Tenant { FullName = "Thuê", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        db.Contracts.Add(new Contract
        {
            ContractCode = "HD", RoomId = room.Id, TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = null,
            MonthlyRent = 2_000_000, ElectricPrice = 3500, WaterPrice = 25000, Status = ContractStatus.Active
        });
        await db.SaveChangesAsync();

        // Chỉ số đầu = cuối kỳ → tiêu thụ 0 → hóa đơn chỉ có tiền phòng.
        var preview = await new InvoiceService(db).PreviewAsync(new CreateInvoiceRequest(room.Id, "2026-02", 100, 100, 10, 10));

        Assert.Single(preview.Items);
        Assert.Equal(2_000_000m, preview.TotalAmount); // chỉ tiền phòng
    }

    [Fact]
    public async Task BillingInfo_UsesContractPricesAndLastInvoiceIndexes()
    {
        var db = TestDb.New();
        var room = new Room { Name = "P101", Price = 2_000_000, Area = 20 };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        var tenant = new Tenant { FullName = "Thuê", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        var contract = new Contract
        {
            ContractCode = "HD", RoomId = room.Id, TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = null,
            MonthlyRent = 2_000_000, ElectricPrice = 3500, WaterPrice = 25000, Status = ContractStatus.Active
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        // Hóa đơn kỳ 2026-08 chốt chỉ số 500/40 → làm chỉ số đầu kỳ cho hóa đơn kế tiếp.
        db.Invoices.Add(new Invoice
        {
            InvoiceCode = "INV-1", ContractId = contract.Id, BillingMonth = "2026-08",
            ElectricOldIndex = 400, ElectricNewIndex = 500, WaterOldIndex = 30, WaterNewIndex = 40,
            TotalAmount = 0, Status = InvoiceStatus.Unpaid
        });
        await db.SaveChangesAsync();

        var info = await new InvoiceService(db).GetBillingInfoAsync(room.Id);

        Assert.Equal(3500m, info.ElectricPrice);
        Assert.Equal(25000m, info.WaterPrice);
        Assert.Equal(2_000_000m, info.MonthlyRent);
        Assert.Equal(500m, info.LastElectricIndex);
        Assert.Equal(40m, info.LastWaterIndex);
    }
}
