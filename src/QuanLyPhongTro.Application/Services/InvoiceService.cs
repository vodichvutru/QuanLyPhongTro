using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Billing;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

public class InvoiceService
{
    private readonly AppDbContext _db;

    public InvoiceService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InvoiceDto>> ListAsync(InvoiceStatus? status = null, string? month = null)
    {
        var q = _db.Invoices.AsNoTracking()
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .Include(i => i.Contract).ThenInclude(c => c.Tenant)
            .AsQueryable();
        if (status.HasValue) q = q.Where(i => i.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(month)) q = q.Where(i => i.BillingMonth == month.Trim());

        var list = await q.OrderByDescending(i => i.BillingMonth).ThenByDescending(i => i.Id).ToListAsync();
        return list.Select(DtoMapper.ToInvoice).ToList();
    }

    public async Task<InvoiceDto> GetAsync(int id)
    {
        var invoice = await QueryDetailedAsync(id)
            ?? throw new AppException("Không tìm thấy hóa đơn.", 404);
        return DtoMapper.ToInvoice(invoice);
    }

    /// <summary>
    /// Tự động tạo hóa đơn cho một phòng theo kỳ (yyyy-MM): tìm hợp đồng còn hiệu lực,
    /// tính điện/nước từ chỉ số đồng hồ, cộng tiền phòng + các khoản khác.
    /// </summary>
    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request)
    {
        var (year, month) = ParseMonth(request.BillingMonth);
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId)
            ?? throw new AppException("Không tìm thấy phòng.", 404);

        var contract = await _db.Contracts
            .Where(c => c.RoomId == request.RoomId && c.Status == ContractStatus.Active
                        && c.StartDate <= monthEnd
                        && (c.EndDate == null || c.EndDate.Value >= monthStart))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync()
            ?? throw new AppException($"Không tìm thấy hợp đồng còn hiệu lực cho phòng {room.Name} trong kỳ {request.BillingMonth}.");

        var existing = await _db.Invoices.FirstOrDefaultAsync(i => i.ContractId == contract.Id && i.BillingMonth == request.BillingMonth);
        if (existing is not null && existing.Status != InvoiceStatus.Cancelled)
            throw new AppException($"Phòng {room.Name} đã có hóa đơn kỳ {request.BillingMonth}.", 409);
        if (existing is not null) // hóa đơn đã hủy → xóa đi để tạo lại
            _db.Invoices.Remove(existing);

        var (opening, closing) = await FindReadingPairAsync(request.RoomId, monthStart, monthEnd);
        // Chưa có chỉ số "đóng kỳ" (còn đang ghi, hoặc chưa ghi) → coi tiêu thụ kỳ này = 0, không được tính ra số âm.
        var electricOpening = opening?.ElectricIndex ?? 0;
        var waterOpening = opening?.WaterIndex ?? 0;
        var (kwh, m3) = InvoiceCalculator.ComputeUsage(
            electricOpening, closing?.ElectricIndex ?? electricOpening,
            waterOpening, closing?.WaterIndex ?? waterOpening);

        var items = InvoiceCalculator.BuildItems(
            room.Name, contract.MonthlyRent, kwh, contract.ElectricPrice, m3, contract.WaterPrice,
            request.ExtraItems);
        var total = InvoiceCalculator.SumItems(items);

        var monthLabel = $"{year:0000}-{month:00}";
        var previousDebt = await SumUnpaidOlderAsync(contract.Id, monthLabel);

        var invoice = new Invoice
        {
            InvoiceCode = $"HD-{year:0000}{month:00}-{room.Name}",
            ContractId = contract.Id,
            BillingMonth = monthLabel,
            IssueDate = DateTime.Now,
            DueDate = request.DueDate ?? monthEnd,
            TotalAmount = total,
            PreviousDebt = previousDebt,
            Status = InvoiceStatus.Unpaid,
            Note = request.Note?.Trim()
        };
        foreach (var item in items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                Name = item.Name, Quantity = item.Quantity, Unit = item.Unit,
                UnitPrice = item.UnitPrice, Amount = item.Amount
            });
        }
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return DtoMapper.ToInvoice(await QueryDetailedAsync(invoice.Id) ?? invoice);
    }

    /// <summary>
    /// Dự toán hóa đơn theo kỳ (không lưu): xem trước các khoản khi "Lập hóa đơn".
    /// Dùng cùng logic của CreateAsync (hợp đồng hiệu lực + cặp chỉ số điện/nước + nợ kỳ trước).
    /// </summary>
    public async Task<InvoicePreviewDto> PreviewAsync(CreateInvoiceRequest request)
    {
        var (year, month) = ParseMonth(request.BillingMonth);
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RoomId)
            ?? throw new AppException("Không tìm thấy phòng.", 404);

        var contract = await _db.Contracts.AsNoTracking()
            .Include(c => c.Tenant)
            .Where(c => c.RoomId == request.RoomId && c.Status == ContractStatus.Active
                        && c.StartDate <= monthEnd
                        && (c.EndDate == null || c.EndDate.Value >= monthStart))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync()
            ?? throw new AppException($"Không tìm thấy hợp đồng còn hiệu lực cho phòng {room.Name} trong kỳ {request.BillingMonth}.");

        var (opening, closing) = await FindReadingPairAsync(request.RoomId, monthStart, monthEnd);
        // Chưa có chỉ số "đóng kỳ" → coi tiêu thụ kỳ này = 0 (không tính ra số âm).
        var electricOpening = opening?.ElectricIndex ?? 0;
        var waterOpening = opening?.WaterIndex ?? 0;
        var (kwh, m3) = InvoiceCalculator.ComputeUsage(
            electricOpening, closing?.ElectricIndex ?? electricOpening,
            waterOpening, closing?.WaterIndex ?? waterOpening);

        var items = InvoiceCalculator.BuildItems(
            room.Name, contract.MonthlyRent, kwh, contract.ElectricPrice, m3, contract.WaterPrice,
            request.ExtraItems);
        var monthLabel = $"{year:0000}-{month:00}";
        var previousDebt = await SumUnpaidOlderAsync(contract.Id, monthLabel);

        return new InvoicePreviewDto(
            room.Name, contract.Tenant?.FullName, monthLabel, InvoiceCalculator.SumItems(items), previousDebt,
            items.Select(s => new InvoiceItemDto(0, s.Name, s.Quantity, s.Unit, s.UnitPrice, s.Amount)).ToList());
    }

    /// <summary>Hủy hóa đơn (chỉ khi chưa có thanh toán).</summary>
    public async Task<InvoiceDto> CancelAsync(int id)
    {
        var invoice = await _db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new AppException("Không tìm thấy hóa đơn.", 404);
        if (invoice.Payments.Count > 0)
            throw new AppException("Không thể hủy hóa đơn đã có thanh toán.", 409);
        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new AppException("Hóa đơn đã được hủy.", 409);

        invoice.Status = InvoiceStatus.Cancelled;
        await _db.SaveChangesAsync();
        return await GetAsync(id);
    }

    /// <summary>Xóa hóa đơn (chỉ khi chưa có thanh toán).</summary>
    public async Task DeleteAsync(int id)
    {
        var invoice = await _db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new AppException("Không tìm thấy hóa đơn.", 404);
        if (invoice.Payments.Count > 0)
            throw new AppException("Không thể xóa hóa đơn đã có thanh toán.", 409);

        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();
    }

    // ---------------- helpers ----------------

    private Task<Invoice?> QueryDetailedAsync(int id) =>
        _db.Invoices.AsNoTracking()
            .Include(i => i.Contract).ThenInclude(c => c.Room)
            .Include(i => i.Contract).ThenInclude(c => c.Tenant)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

    private static (int Year, int Month) ParseMonth(string month)
    {
        var parts = month.Trim().Split('-');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var m) ||
            year < 2000 || year > 2100 || m < 1 || m > 12)
        {
            throw new AppException("Kỳ hóa đơn phải có dạng yyyy-MM (VD: 2026-08).");
        }
        return (year, m);
    }

    /// <summary>
    /// Chọn cặp chỉ số: mở đầu = bản ghi mới nhất tại hoặc trước đầu kỳ;
    /// kết thúc = bản ghi sau đó trong kỳ. Chưa đủ dữ liệu thì trả null (tính 0).
    /// </summary>
    private async Task<(MeterReading? Opening, MeterReading? Closing)> FindReadingPairAsync(int roomId, DateTime monthStart, DateTime monthEnd)
    {
        var readings = await _db.MeterReadings.AsNoTracking()
            .Where(m => m.RoomId == roomId)
            .OrderBy(m => m.ReadingDate).ThenBy(m => m.Id)
            .ToListAsync();

        var opening = readings.LastOrDefault(m => m.ReadingDate <= monthStart);
        var closing = opening is null
            ? null
            : readings.LastOrDefault(m => m.ReadingDate > opening!.ReadingDate && m.ReadingDate <= monthEnd);
        return (opening, closing);
    }

    private async Task<decimal> SumUnpaidOlderAsync(int contractId, string monthLabel)
    {
        var older = await _db.Invoices.AsNoTracking()
            .Where(i => i.ContractId == contractId && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();
        return older
            .Where(i => string.CompareOrdinal(i.BillingMonth, monthLabel) < 0)
            .Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
    }
}
