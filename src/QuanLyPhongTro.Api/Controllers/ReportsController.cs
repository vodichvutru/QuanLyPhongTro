using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Owner")]
public class ReportsController : BaseController
{
    private readonly ReportService _reports;

    public ReportsController(ReportService reports) => _reports = reports;

    /// <summary>Bảng tổng quan: phòng, người thuê, công nợ, doanh thu tháng này.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard() => Ok(await _reports.GetDashboardAsync());

    /// <summary>Doanh thu đã thu theo tháng. VD: ?from=2026-01-01&amp;to=2026-12-31 (mặc định 6 tháng gần).</summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<IReadOnlyList<RevenuePoint>>> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _reports.GetRevenueAsync(from, to));

    /// <summary>Danh sách công nợ (hóa đơn chưa trả đủ).</summary>
    [HttpGet("debt")]
    public async Task<ActionResult<IReadOnlyList<DebtItemDto>>> Debt() => Ok(await _reports.GetDebtAsync());

    /// <summary>Công nợ gộp theo phòng.</summary>
    [HttpGet("debt-by-room")]
    public async Task<ActionResult<IReadOnlyList<DebtByRoomDto>>> DebtByRoom() => Ok(await _reports.GetDebtByRoomAsync());

    /// <summary>Xuất doanh thu ra file CSV (mở bằng Excel).</summary>
    [HttpGet("revenue/export")]
    public async Task<IActionResult> ExportRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var bytes = await _reports.ExportRevenueCsvAsync(from, to);
        return File(bytes, "text/csv; charset=utf-8", $"doanh-thu-{DateTime.Now:yyyyMMdd}.csv");
    }
}
