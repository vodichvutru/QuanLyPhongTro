using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

/// <summary>Báo cáo thống kê cho chủ trọ/Admin (US-011).</summary>
public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var now = DateTime.Now;
        var rooms = await _db.Rooms.AsNoTracking().ToListAsync();
        var tenants = await _db.Tenants.AsNoTracking().ToListAsync();
        var openInvoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();
        var paymentsThisMonth = await _db.Payments.AsNoTracking()
            .Where(p => p.PaidAt.Year == now.Year && p.PaidAt.Month == now.Month)
            .ToListAsync();

        return new DashboardDto(
            TotalRooms: rooms.Count,
            RentedRooms: rooms.Count(r => r.Status == RoomStatus.Rented),
            AvailableRooms: rooms.Count(r => r.Status == RoomStatus.Available),
            MaintenanceRooms: rooms.Count(r => r.Status == RoomStatus.Maintenance),
            TotalTenants: tenants.Count,
            ActiveTenants: tenants.Count(t => t.IsActive),
            OpenInvoices: openInvoices.Count,
            TotalOutstanding: InvoiceCalculatorRound(openInvoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount))),
            RevenueThisMonth: InvoiceCalculatorRound(paymentsThisMonth.Sum(p => p.Amount)));
    }

    /// <summary>Doanh thu theo tháng (từ các khoản đã thu) trong khoảng [from, to].</summary>
    public async Task<IReadOnlyList<RevenuePoint>> GetRevenueAsync(DateTime? from = null, DateTime? to = null)
    {
        var start = new DateTime((from ?? DateTime.Now.AddMonths(-5)).Year, (from ?? DateTime.Now.AddMonths(-5)).Month, 1);
        var end = to.HasValue
            ? new DateTime(to.Value.Year, to.Value.Month, DateTime.DaysInMonth(to.Value.Year, to.Value.Month))
            : DateTime.Now.Date;

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.PaidAt >= start && p.PaidAt <= end.AddDays(1))
            .ToListAsync();

        var byMonth = payments
            .GroupBy(p => new DateTime(p.PaidAt.Year, p.PaidAt.Month, 1))
            .ToDictionary(g => g.Key, g => InvoiceCalculatorRound(g.Sum(p => p.Amount)));

        var result = new List<RevenuePoint>();
        for (var m = start; m <= end; m = m.AddMonths(1))
        {
            result.Add(new RevenuePoint($"{m:yyyy-MM}", byMonth.GetValueOrDefault(m, 0)));
        }
        return result;
    }

    /// <summary>Công nợ gộp theo phòng (US-011 AC-03).</summary>
    public async Task<IReadOnlyList<DebtByRoomDto>> GetDebtByRoomAsync()
    {
        var debt = await GetDebtAsync();
        return debt
            .GroupBy(d => d.RoomName)
            .Select(g => new DebtByRoomDto(
                g.Key,
                g.Count(),
                InvoiceCalculatorRound(g.Sum(x => x.Remaining))))
            .OrderByDescending(x => x.DebtAmount)
            .ToList();
    }

    /// <summary>Danh sách công nợ: hóa đơn chưa thanh toán đủ (không gồm đã hủy/đã trả đủ).</summary>
    public async Task<IReadOnlyList<DebtItemDto>> GetDebtAsync()
    {
        var invoices = await _db.Invoices.AsNoTracking()
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .Include(i => i.Contract).ThenInclude(c => c.Tenant)
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        return invoices
            .Select(i => new DebtItemDto(
                i.Id, i.InvoiceCode, i.Contract.Room.Name, i.Contract.Tenant.FullName,
                i.BillingMonth, i.DueDate, i.TotalAmount, i.PaidAmount,
                InvoiceCalculatorRound(Math.Max(0, i.TotalAmount - i.PaidAmount)),
                DtoMapper.DisplayName(i.Status)))
            .OrderBy(d => d.DueDate ?? DateTime.MaxValue)
            .ThenBy(d => d.BillingMonth)
            .ToList();
    }

    /// <summary>Xuất doanh thu theo tháng ra file CSV (Excel mở được).</summary>
    public async Task<byte[]> ExportRevenueCsvAsync(DateTime? from = null, DateTime? to = null)
    {
        var rows = await GetRevenueAsync(from, to);
        var sb = new System.Text.StringBuilder();
        sb.Append("﻿"); // BOM cho Excel hiển thị tiếng Việt
        sb.AppendLine("Tháng,Doanh thu (VNĐ)");
        foreach (var row in rows)
            sb.AppendLine($"{row.Month},{row.Revenue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static decimal InvoiceCalculatorRound(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
