# Cấu trúc Database — Hệ thống Quản lý Phòng trọ

CSDL `quanlyphongtro` (MySQL 8) được tạo **code-first** bằng EF Core 8 + Pomelo. Khi chạy chương trình:
`EnsureCreated()` tạo schema (nếu DB mới) + `EnsureSchemaUpToDateAsync()` đồng bộ bảng bổ sung + `DbSeeder` nạp dữ liệu mẫu.

## Các bảng

| Bảng | Mục đích | Quan hệ chính |
|------|----------|----------------|
| `Users` | Tài khoản đăng nhập (username, bcrypt hash) | 1–* `UserRoles` |
| `Roles` | Vai trò: `Admin`, `Owner`, `Tenant` | *–* `Users` (qua `UserRoles`) |
| `UserRoles` | Liên kết User–Role | — |
| `Rooms` | Phòng trọ: mã, tầng, diện tích, giá, trạng thái | 1–* `Contracts`, `MeterReadings`, `RepairRequests` |
| `Tenants` | Hồ sơ người thuê; `UserId` liên kết tài khoản (nếu có) | 1–* `Contracts`; có thể 1–1 `Users` |
| `Contracts` | Hợp đồng thuê: phòng–người thuê, tiền phòng, giá điện/nước | *–1 `Rooms`, *–1 `Tenants`; 1–* `Invoices` |
| `MeterReadings` | Chỉ số đồng hồ điện (kWh) + nước (m³) theo phòng | *–1 `Rooms` |
| `Invoices` | Hóa đơn theo kỳ `yyyy-MM`: tổng tiền, đã thu, công nợ kỳ trước | *–1 `Contracts`; 1–* `InvoiceItems`, 1–* `Payments` |
| `InvoiceItems` | Dòng chi tiết hóa đơn (tiền phòng, điện, nước, phụ phí) | *–1 `Invoices` |
| `Payments` | Phiếu thu tiền; tự cập nhật trạng thái hóa đơn | *–1 `Invoices` |
| `RepairRequests` | Yêu cầu sửa chữa: phòng–người thuê, trạng thái, phản hồi, chi phí | *–1 `Rooms`; *–1 `Tenants` |

## Enum → trạng thái

- **Rooms.Status**: `Available`, `Rented`, `Maintenance`
- **Contracts.Status**: `Active`, `Terminated`, `Expired` (tự đánh dấu hết hạn khi quá ngày kết thúc)
- **Invoices.Status**: `Unpaid` → `PartiallyPaid` → `Paid` (tự động khi thu đủ); `Cancelled`
- **Payments.Method**: `Cash`, `BankTransfer`, `Momo`, `VnPay`, `Other`
- **RepairRequests.Status**: `Pending` → `Approved` → `InProgress` → `Completed`; `Rejected`, `Cancelled`

## Quy tắc nghiệp vụ tiêu biểu (thực thi tại Service/API)

- Phòng chỉ có **1 hợp đồng đang hiệu lực** tại một thời điểm → chặn tạo hợp đồng trùng kỳ (409).
- Lập hóa đơn tự tìm hợp đồng còn hiệu lực trong kỳ; **1 phòng–1 kỳ = 1 hóa đơn** (409 nếu trùng, cho phép tạo lại khi hóa đơn cũ đã hủy).
- Tiêu thụ điện/nước = chỉ số cuối kỳ − đầu kỳ; không được âm.
- Không thanh toán hóa đơn đã hủy; không trả vượt số còn lại; thu đủ → tự sang `Paid`.
- Người thuê **chỉ** thao tác trên dữ liệu của mình (qua `Tenant.UserId`); chỉ gửi yêu cầu sửa chữa cho phòng đang thuê (BR-08).

## Seed mẫu

Vai trò `Admin`/`Owner`/`Tenant`; tài khoản `admin`/`chutro`/`tenant1` (mật khẩu `123456`); 3 phòng P101/P102/P201; 1 người thuê mẫu liên kết `tenant1`.
