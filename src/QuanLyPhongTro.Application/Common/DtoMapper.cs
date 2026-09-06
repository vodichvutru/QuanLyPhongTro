using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Application.Common;

/// <summary>Ánh xạ Entity -> DTO thủ công (tránh phụ thuộc AutoMapper).</summary>
public static class DtoMapper
{
    public static UserDto ToUser(User u) => new(
        u.Id, u.Username, u.FullName, u.Email, u.Phone, u.IsActive,
        u.UserRoles.Select(ur => ur.Role.Code).OrderBy(c => c).ToList());

    public static RoomDto ToRoom(Room r) => new(
        r.Id, r.Name, r.Floor, r.Area, r.Price, r.MaxPeople, r.Note, r.Status,
        DisplayName(r.Status), r.CreatedAt);

    public static TenantDto ToTenant(Tenant t) => new(
        t.Id, t.FullName, t.Phone, t.IdentityNumber, t.Email, t.Address, t.Note,
        t.IsActive, t.UserId.HasValue, t.CreatedAt);

    public static ContractDto ToContract(Contract c) => new(
        c.Id, c.ContractCode, c.RoomId, c.Room.Name, c.TenantId, c.Tenant.FullName,
        c.StartDate, c.EndDate, c.MonthlyRent, c.Deposit, c.ElectricPrice, c.WaterPrice,
        c.Status, DisplayName(c.Status), c.Note, c.CreatedAt);

    public static MeterReadingDto ToMeterReading(MeterReading m, string roomName) => new(
        m.Id, m.RoomId, roomName, m.ReadingDate, m.ElectricIndex, m.WaterIndex, m.Note, m.CreatedAt);

    public static InvoiceItemDto ToItem(InvoiceItem i) => new(
        i.Id, i.Name, i.Quantity, i.Unit, i.UnitPrice, i.Amount);

    public static PaymentDto ToPayment(Payment p) => new(
        p.Id, p.InvoiceId, p.Amount, p.Method, DisplayName(p.Method), p.PaidAt, p.Reference, p.Note, p.CreatedAt);

    public static InvoiceDto ToInvoice(Invoice inv) => new(
        inv.Id, inv.InvoiceCode, inv.ContractId, inv.Contract.RoomId, inv.Contract.Room.Name,
        inv.Contract.TenantId, inv.Contract.Tenant.FullName, inv.BillingMonth, inv.IssueDate,
        inv.DueDate, inv.TotalAmount, inv.PaidAmount, inv.PreviousDebt, inv.Status,
        DisplayName(inv.Status), inv.Note,
        inv.Items.OrderBy(i => i.Id).Select(ToItem).ToList(),
        inv.Payments.OrderByDescending(p => p.PaidAt).Select(ToPayment).ToList());

    public static string DisplayName(RoomStatus s) => s switch
    {
        RoomStatus.Available => "Trống",
        RoomStatus.Rented => "Đang cho thuê",
        RoomStatus.Maintenance => "Bảo trì",
        _ => s.ToString()
    };

    public static string DisplayName(ContractStatus s) => s switch
    {
        ContractStatus.Active => "Đang hiệu lực",
        ContractStatus.Terminated => "Đã chấm dứt",
        ContractStatus.Expired => "Hết hạn",
        _ => s.ToString()
    };

    public static string DisplayName(InvoiceStatus s) => s switch
    {
        InvoiceStatus.Unpaid => "Chưa thanh toán",
        InvoiceStatus.PartiallyPaid => "Thanh toán một phần",
        InvoiceStatus.Paid => "Đã thanh toán",
        InvoiceStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    public static RepairRequestDto ToRepair(RepairRequest r) => new(
        r.Id, r.RoomId, r.Room.Name, r.TenantId, r.Tenant?.FullName, r.Subject, r.Description,
        r.Status, DisplayName(r.Status), r.OwnerNote, r.Cost, r.CreatedByUserId, r.CreatedByName,
        r.CreatedAt, r.HandledAt, r.HandledByName);

    public static string DisplayName(RepairStatus s) => s switch
    {
        RepairStatus.Pending => "Chờ xử lý",
        RepairStatus.Approved => "Đã tiếp nhận",
        RepairStatus.InProgress => "Đang xử lý",
        RepairStatus.Completed => "Đã hoàn thành",
        RepairStatus.Rejected => "Từ chối",
        RepairStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    public static string DisplayName(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.Momo => "Momo",
        PaymentMethod.VnPay => "VNPay",
        _ => "Khác"
    };
}
