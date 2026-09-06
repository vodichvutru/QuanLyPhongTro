using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Tests;

public class PaymentServiceTests
{
    private const decimal Total = 1_000_000;

    [Fact]
    public async Task PartialPayment_MarksPartiallyPaid()
    {
        var db = TestDb.New();
        db.Invoices.Add(new Invoice { InvoiceCode = "H1", ContractId = 1, BillingMonth = "2026-08", TotalAmount = Total, Status = InvoiceStatus.Unpaid });
        await db.SaveChangesAsync();
        var id = (await db.Invoices.FirstAsync()).Id;

        var svc = new PaymentService(db);
        await svc.CreateAsync(new CreatePaymentRequest(id, 400_000, PaymentMethod.Cash));

        var upd = await db.Invoices.FindAsync(id);
        Assert.Equal(InvoiceStatus.PartiallyPaid, upd!.Status);
        Assert.Equal(400_000, upd.PaidAmount);
    }

    [Fact]
    public async Task FullPayments_AfterPartial_MarksPaid()
    {
        var db = TestDb.New();
        db.Invoices.Add(new Invoice { InvoiceCode = "H1", ContractId = 1, BillingMonth = "2026-08", TotalAmount = Total, Status = InvoiceStatus.Unpaid });
        await db.SaveChangesAsync();
        var id = (await db.Invoices.FirstAsync()).Id;
        var svc = new PaymentService(db);

        await svc.CreateAsync(new CreatePaymentRequest(id, 400_000, PaymentMethod.Cash));
        await svc.CreateAsync(new CreatePaymentRequest(id, 600_000, PaymentMethod.Cash));

        var upd = await db.Invoices.FindAsync(id);
        Assert.Equal(InvoiceStatus.Paid, upd!.Status);
        Assert.Equal(Total, upd.PaidAmount);
    }

    [Fact]
    public async Task Overpayment_Throws400()
    {
        var db = TestDb.New();
        db.Invoices.Add(new Invoice { InvoiceCode = "H1", ContractId = 1, BillingMonth = "2026-08", TotalAmount = Total, Status = InvoiceStatus.Unpaid });
        await db.SaveChangesAsync();
        var id = (await db.Invoices.FirstAsync()).Id;

        var svc = new PaymentService(db);
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateAsync(new CreatePaymentRequest(id, Total + 1, PaymentMethod.Cash)));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task PayCancelledInvoice_Throws409()
    {
        var db = TestDb.New();
        db.Invoices.Add(new Invoice { InvoiceCode = "H2", ContractId = 1, BillingMonth = "2026-08", TotalAmount = Total, Status = InvoiceStatus.Cancelled });
        await db.SaveChangesAsync();
        var id = (await db.Invoices.FirstAsync()).Id;

        var svc = new PaymentService(db);
        var ex = await Assert.ThrowsAsync<AppException>(
            () => svc.CreateAsync(new CreatePaymentRequest(id, 100_000, PaymentMethod.Cash)));
        Assert.Equal(409, ex.StatusCode);
    }
}
