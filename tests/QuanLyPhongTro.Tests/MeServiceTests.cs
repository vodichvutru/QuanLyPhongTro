using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Tests;

/// <summary>BR-07: người thuê chỉ xem được dữ liệu thuộc tài khoản của mình.</summary>
public class MeServiceTests
{
    private async Task<(AppDbContext db, int userA)> SeedTwoTenantsAsync()
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

        var contractA = new Contract
        {
            ContractCode = "HD-A", RoomId = roomA.Id, TenantId = tA.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 1, 1),
            MonthlyRent = 2_000_000, Status = ContractStatus.Active
        };
        var contractB = new Contract
        {
            ContractCode = "HD-B", RoomId = roomB.Id, TenantId = tB.Id,
            StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 1, 1),
            MonthlyRent = 2_500_000, Status = ContractStatus.Active
        };
        db.Contracts.AddRange(contractA, contractB);
        await db.SaveChangesAsync();

        var invA = new Invoice { InvoiceCode = "INV-A", Contract = contractA, ContractId = contractA.Id, BillingMonth = "2026-08", TotalAmount = 2_000_000, Status = InvoiceStatus.Unpaid };
        var invB = new Invoice { InvoiceCode = "INV-B", Contract = contractB, ContractId = contractB.Id, BillingMonth = "2026-08", TotalAmount = 2_500_000, Status = InvoiceStatus.Unpaid };
        db.Invoices.AddRange(invA, invB);
        await db.SaveChangesAsync();

        return (db, userA.Id);
    }

    [Fact]
    public async Task MyInvoices_ReturnsOnlyOwnData()
    {
        var (db, userA) = await SeedTwoTenantsAsync();
        var svc = new MeService(db);

        var invoices = await svc.GetMyInvoicesAsync(userA);

        Assert.Single(invoices);
        Assert.Equal("INV-A", invoices[0].InvoiceCode);
    }

    [Fact]
    public async Task MyInvoice_OtherTenantInvoice_Throws404()
    {
        var (db, userA) = await SeedTwoTenantsAsync();
        var svc = new MeService(db);

        var invB = db.Invoices.First(i => i.InvoiceCode == "INV-B");
        await Assert.ThrowsAsync<AppException>(() => svc.GetMyInvoiceAsync(userA, invB.Id));
    }
}
