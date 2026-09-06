using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Application.Dtos;

// ---------------- Auth / Users ----------------

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    int Id,
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<string> Roles);

public record CreateUserRequest(
    string Username,
    string Password,
    string FullName,
    string? Email = null,
    string? Phone = null,
    List<string>? Roles = null);

public record UpdateUserRolesRequest(List<string> Roles);

public record SetUserActiveRequest(bool IsActive);

public record RoleDto(int Id, string Code, string Name, string? Description);

// ---------------- Rooms ----------------

public record RoomDto(
    int Id,
    string Name,
    string? Floor,
    decimal Area,
    decimal Price,
    int MaxPeople,
    string? Note,
    RoomStatus Status,
    string StatusName,
    DateTime CreatedAt);

public record CreateRoomRequest(
    string Name,
    string? Floor,
    decimal Area,
    decimal Price,
    int MaxPeople,
    string? Note);

public record UpdateRoomRequest(
    string Name,
    string? Floor,
    decimal Area,
    decimal Price,
    int MaxPeople,
    string? Note,
    RoomStatus? Status = null);

// ---------------- Tenants ----------------

public record TenantDto(
    int Id,
    string FullName,
    string? Phone,
    string? IdentityNumber,
    string? Email,
    string? Address,
    string? Note,
    bool IsActive,
    bool HasAccount,
    DateTime CreatedAt);

public record CreateTenantRequest(
    string FullName,
    string? Phone = null,
    string? IdentityNumber = null,
    string? Email = null,
    string? Address = null,
    string? Note = null);

public record UpdateTenantRequest(
    string FullName,
    string? Phone = null,
    string? IdentityNumber = null,
    string? Email = null,
    string? Address = null,
    string? Note = null,
    bool IsActive = true);

// ---------------- Contracts ----------------

public record ContractDto(
    int Id,
    string ContractCode,
    int RoomId,
    string RoomName,
    int TenantId,
    string TenantName,
    DateTime StartDate,
    DateTime? EndDate,
    decimal MonthlyRent,
    decimal Deposit,
    decimal ElectricPrice,
    decimal WaterPrice,
    ContractStatus Status,
    string StatusName,
    string? Note,
    DateTime CreatedAt);

public record CreateContractRequest(
    int RoomId,
    int TenantId,
    DateTime StartDate,
    DateTime? EndDate = null,
    decimal MonthlyRent = 0,
    decimal Deposit = 0,
    decimal ElectricPrice = 3500,
    decimal WaterPrice = 25000,
    string? Note = null);

public record TerminateContractRequest(DateTime? TerminatedAt = null, string? Reason = null);

// ---------------- Meter readings ----------------

public record MeterReadingDto(
    int Id,
    int RoomId,
    string RoomName,
    DateTime ReadingDate,
    decimal ElectricIndex,
    decimal WaterIndex,
    string? Note,
    DateTime CreatedAt);

public record CreateMeterReadingRequest(
    int RoomId,
    DateTime ReadingDate,
    decimal ElectricIndex,
    decimal WaterIndex,
    string? Note = null);

// ---------------- Invoices ----------------

public class ExtraFeeLine
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public ExtraFeeLine() { }
    public ExtraFeeLine(string name, decimal amount) { Name = name; Amount = amount; }
}

public record InvoiceDto(
    int Id,
    string InvoiceCode,
    int ContractId,
    int RoomId,
    string RoomName,
    int TenantId,
    string TenantName,
    string BillingMonth,
    DateTime IssueDate,
    DateTime? DueDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal PreviousDebt,
    InvoiceStatus Status,
    string StatusName,
    string? Note,
    IReadOnlyList<InvoiceItemDto> Items,
    IReadOnlyList<PaymentDto> Payments);

public record InvoiceItemDto(
    int Id,
    string Name,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal Amount);

public record CreateInvoiceRequest(
    int RoomId,
    string BillingMonth,
    DateTime? DueDate = null,
    string? Note = null,
    List<ExtraFeeLine>? ExtraItems = null);

// ---------------- Payments ----------------

public record PaymentDto(
    int Id,
    int InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string MethodName,
    DateTime PaidAt,
    string? Reference,
    string? Note,
    DateTime CreatedAt);

public record CreatePaymentRequest(
    int InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    DateTime? PaidAt = null,
    string? Reference = null,
    string? Note = null);

// ---------------- Repair requests ----------------

public record RepairRequestDto(
    int Id,
    int RoomId,
    string RoomName,
    int? TenantId,
    string? TenantName,
    string Subject,
    string? Description,
    RepairStatus Status,
    string StatusName,
    string? OwnerNote,
    decimal? Cost,
    int? CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime? HandledAt,
    string? HandledByName);

public record CreateRepairRequestRequest(
    int RoomId,
    string Subject,
    string? Description = null,
    int? TenantId = null);

public record CreateMyRepairRequestRequest(
    int RoomId,
    string Subject,
    string? Description = null);

public record UpdateRepairStatusRequest(
    RepairStatus Status,
    string? OwnerNote = null,
    decimal? Cost = null);

// ---------------- Reports ----------------

public record DashboardDto(
    int TotalRooms,
    int RentedRooms,
    int AvailableRooms,
    int MaintenanceRooms,
    int TotalTenants,
    int ActiveTenants,
    int OpenInvoices,
    decimal TotalOutstanding,
    decimal RevenueThisMonth);

public record RevenuePoint(string Month, decimal Revenue);

public record DebtItemDto(
    int InvoiceId,
    string InvoiceCode,
    string RoomName,
    string TenantName,
    string BillingMonth,
    DateTime? DueDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal Remaining,
    string StatusName);

public record DebtByRoomDto(
    string RoomName,
    int OpenInvoices,
    decimal DebtAmount);
