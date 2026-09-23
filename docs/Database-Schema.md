# Cấu trúc Database — Hệ thống Quản lý Phòng trọ

CSDL `quanlyphongtro` (MySQL 8) được tạo **code-first** bằng EF Core 8 + Pomelo. Khi chạy chương trình:
`EnsureCreated()` tạo schema (nếu DB mới) + `EnsureSchemaUpToDateAsync()` đồng bộ bảng bổ sung + `DbSeeder` nạp dữ liệu mẫu.

## Các bảng

| Bảng | Mục đích | Quan hệ chính |
|------|----------|----------------|
| `Users` | Tài khoản đăng nhập (username, bcrypt hash) | 1–* `UserRoles` |
| `Roles` | Vai trò: `Admin`, `Owner`, `Tenant` | *–* `Users` (qua `UserRoles`) |
| `UserRoles` | Liên kết User–Role | — |
| `Rooms` | Phòng trọ: mã, tầng, diện tích, giá, trạng thái | 1–* `Contracts`, `RepairRequests` |
| `Tenants` | Hồ sơ người thuê; `UserId` liên kết tài khoản (nếu có) | 1–* `Contracts`; có thể 1–1 `Users` |
| `Contracts` | Hợp đồng thuê: phòng–người thuê, tiền phòng, giá điện/nước | *–1 `Rooms`, *–1 `Tenants`; 1–* `Invoices` |
| `Invoices` | Hóa đơn theo kỳ `yyyy-MM`: **chỉ số điện/nước đầu–cuối kỳ**, tổng tiền, đã thu, công nợ kỳ trước | *–1 `Contracts`; 1–* `InvoiceItems`, 1–* `Payments` |
| `InvoiceItems` | Dòng chi tiết hóa đơn (tiền phòng, điện, nước, phụ phí) | *–1 `Invoices` |
| `Payments` | Phiếu thu tiền; `Status` = Pending (người thuê báo, chờ xác nhận) / Confirmed / Rejected. Chỉ khoản **Confirmed** mới tính vào số đã thu | *–1 `Invoices` |
| `Notifications` | Thông báo cho vai trò (`TargetRole`) hoặc một tài khoản (`TargetUserId`): người thuê báo thanh toán, gửi yêu cầu sửa chữa, chủ trọ xác nhận… | — |
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
- Chỉ số điện/nước được **nhập trực tiếp khi lập hóa đơn** và lưu trên hóa đơn (`ElectricOldIndex`/`ElectricNewIndex`, `WaterOldIndex`/`WaterNewIndex`); tiêu thụ = cuối kỳ − đầu kỳ, không được âm. Chỉ số đầu kỳ gợi ý sẵn = chỉ số cuối kỳ của hóa đơn gần nhất.
- Không thanh toán hóa đơn đã hủy; không trả vượt số còn lại; thu đủ → tự sang `Paid`.
- **Thanh toán 2 bước**: người thuê bấm thanh toán → tạo khoản `Pending` (chưa tính vào số đã thu) + **thông báo cho chủ trọ**; chủ trọ kiểm tra rồi **xác nhận** → khoản thành `Confirmed`, hóa đơn mới cập nhật `Paid`/`PartiallyPaid` + thông báo lại cho người thuê. Chủ trọ tự ghi nhận tiền mặt thì `Confirmed` ngay.
- Mọi sự kiện từ người thuê (báo thanh toán, gửi yêu cầu sửa chữa) đều sinh **thông báo** cho chủ trọ.
- Người thuê **chỉ** thao tác trên dữ liệu của mình (qua `Tenant.UserId`); chỉ gửi yêu cầu sửa chữa cho phòng đang thuê (BR-08).

## Seed mẫu

Vai trò `Admin`/`Owner`/`Tenant`; tài khoản `admin`/`chutro`/`nguyenvana`… (mật khẩu `123456`); ~10 phòng; 7 người thuê có tài khoản; hợp đồng, ~30 hóa đơn trải 5 tháng (kèm chỉ số điện/nước), thanh toán và yêu cầu sửa chữa.

## Ghi chú migration

Bảng `MeterReadings` cũ đã được **gộp vào `Invoices`** (chỉ số lưu trên hóa đơn). Khi khởi động, `EnsureSchemaUpToDateAsync()` tự: thêm 4 cột chỉ số cho `Invoices` → chuyển dữ liệu từ `MeterReadings` (nếu còn) → bỏ bảng `MeterReadings`.

**Người thuê của phòng không lưu trên `Rooms`** mà suy ra từ hợp đồng đang hiệu lực (`Contracts`). Muốn gán người thuê cho phòng → lập Hợp đồng (tạo hợp đồng tự đặt phòng sang `Rented`; chấm dứt/hết hạn tự trả về `Available`). Một người thuê chỉ có **1 hợp đồng hiệu lực** tại một thời điểm.
