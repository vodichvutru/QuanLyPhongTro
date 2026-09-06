namespace QuanLyPhongTro.Core.Enums;

public enum RoomStatus
{
    Available = 1,
    Rented = 2,
    Maintenance = 3
}

public enum ContractStatus
{
    Active = 1,
    Terminated = 2,
    Expired = 3
}

public enum InvoiceStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Momo = 3,
    VnPay = 4,
    Other = 5
}

public enum RepairStatus
{
    Pending = 1,
    Approved = 2,
    InProgress = 3,
    Completed = 4,
    Rejected = 5,
    Cancelled = 6
}
