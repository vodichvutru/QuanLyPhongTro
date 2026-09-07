using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Tests;

/// <summary>
/// Thanh toán trực tuyến của người thuê (ghi nhận ngay, trả đủ) + dự toán hóa đơn khi lập
/// + thông tin giá điện/nước cho màn hình ghi chỉ số.
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
    public async Task TenantPay_FullAmount_MarksInvoicePaid_AndRecordsOnePayment()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db);

        var payment = await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId,
            new CreateMyPaymentRequest(PaymentMethod.Momo, Reference: "WEB-TEST-1"));

        Assert.Equal(2_000_000m, payment.Amount);
        Assert.StartsWith("WEB-TEST-1", payment.Reference);
        var invoice = await seed.Db.Invoices.AsNoTracking().SingleAsync(i => i.Id == seed.InvoiceAId);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(2_000_000m, invoice.PaidAmount);
        Assert.Equal(1, await seed.Db.Payments.CountAsync(p => p.InvoiceId == seed.InvoiceAId));
    }

    [Fact]
    public async Task TenantPay_EmptyReference_IsGeneratedWithWebPrefix()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db);

        var payment = await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.VnPay));

        Assert.StartsWith("WEB-", payment.Reference);
    }

    [Fact]
    public async Task TenantPay_OtherTenantsInvoice_Throws404()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db);

        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateByTenantAsync(seed.UserA, seed.InvoiceBId, new CreateMyPaymentRequest(PaymentMethod.Momo)));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task TenantPay_AlreadyPaidInvoice_Throws()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db);
        await svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.Momo));

        await Assert.ThrowsAsync<AppException>(
            () => svc.CreateByTenantAsync(seed.UserA, seed.InvoiceAId, new CreateMyPaymentRequest(PaymentMethod.Momo)));
    }

    [Fact]
    public async Task TenantPay_CashMethod_IsRejected()
    {
        var seed = await SeedTwoTenantsWithInvoicesAsync();
        var svc = new PaymentService(seed.Db);

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

        var svc = new PaymentService(db);
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

        db.MeterReadings.AddRange(
            new MeterReading { RoomId = room.Id, ReadingDate = new DateTime(2026, 1, 31), ElectricIndex = 100, WaterIndex = 10 },
            new MeterReading { RoomId = room.Id, ReadingDate = new DateTime(2026, 2, 10), ElectricIndex = 150, WaterIndex = 13 });
        await db.SaveChangesAsync();

        var svc = new InvoiceService(db);
        var preview = await svc.PreviewAsync(new CreateInvoiceRequest(room.Id, "2026-02"));

        Assert.Equal("P101", preview.RoomName);
        Assert.Equal(3, preview.Items.Count);
        // Tiền phòng 2.000.000 + điện 50×3500 + nước 3×25000
        Assert.Equal(2_250_000m, preview.TotalAmount);
        Assert.Equal(0, await db.Invoices.CountAsync()); // chưa lưu hóa đơn nào
    }

    [Fact]
    public async Task InvoicePreview_NoClosingReading_TreatsUsageAsZero_NoCrash()
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
        // Chỉ có chỉ số đầu kỳ (tháng 2 chưa ghi chỉ số đóng) → không được crash, tiêu thụ = 0.
        db.MeterReadings.Add(new MeterReading { RoomId = room.Id, ReadingDate = new DateTime(2026, 1, 31), ElectricIndex = 100, WaterIndex = 10 });
        await db.SaveChangesAsync();

        var preview = await new InvoiceService(db).PreviewAsync(new CreateInvoiceRequest(room.Id, "2026-02"));

        Assert.Single(preview.Items);
        Assert.Equal(2_000_000m, preview.TotalAmount); // chỉ tiền phòng
    }

    [Fact]
    public async Task MeterBillingInfo_ReturnsContractPricesAndLastReading()
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
        db.MeterReadings.Add(new MeterReading { RoomId = room.Id, ReadingDate = new DateTime(2026, 8, 1), ElectricIndex = 500, WaterIndex = 40 });
        await db.SaveChangesAsync();

        var info = await new MeterReadingService(db).GetBillingInfoAsync(room.Id);

        Assert.Equal(3500m, info.ElectricPrice);
        Assert.Equal(25000m, info.WaterPrice);
        Assert.Equal(500m, info.LastElectricIndex);
        Assert.Equal(40m, info.LastWaterIndex);
    }
}
