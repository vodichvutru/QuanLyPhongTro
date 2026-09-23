# Security Review & Pentest — Quản Lý Phòng Trọ

Phạm vi: backend ASP.NET Core 8 Web API + MySQL 8, kiến trúc JWT/RBAC. Đây là kết quả rà soát bảo mật (code review + kiểm thử bảo mật) phục vụ tiêu chí **Security review & pentest**.

---

## 1. Kiến trúc bảo mật (tóm tắt)

| Cơ chế | Mô tả |
| --- | --- |
| Xác thực | JWT HS256, access 24h + refresh 7 ngày; validate issuer/audience/lifetime |
| Phân quyền | RBAC 3 vai trò Admin/Owner/Tenant; **default deny** (`[Authorize]` mọi controller) |
| Mật khẩu | Hash **BCrypt** (không lưu/lộ plaintext) |
| Chống truy cập chéo (IDOR) | `tenant_id` suy ra server-side từ `UserId` trong JWT, không tin tham số client |
| SQL injection | EF Core LINQ parameterized, không ghép chuỗi SQL từ input |
| Nhất quán tài chính | Transaction + unique constraint chống trùng hóa đơn/kỳ |

## 2. Findings (phát hiện)

| # | Mức độ | Vị trí | Phát hiện | Khuyến nghị | Trạng thái |
| --- | --- | --- | --- | --- | --- |
| S-01 | **High** | `appsettings.json` | Connection string MySQL (`Password=123456`) và JWT secret commit dạng plaintext trong mã nguồn | Chuyển sang biến môi trường / user-secrets / secrets manager; không commit secret | Mở |
| S-02 | **Medium** | `Program.cs` | Fallback JWT secret hardcoded (`QuanLyPhongTro_Secret_Key_0123456789`) khi thiếu cấu hình — secret công khai, kẻ tấn công có thể tự ký token | Bỏ fallback, bắt buộc đọc secret từ config/env và fail nhanh nếu thiếu | Mở |
| S-03 | **Medium** | `Program.cs` | CORS cấu hình `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` — quá rộng | Allowlist origin cụ thể (dùng `appsettings`), thu hẹp method/header | Mở |
| S-04 | **Medium** | `AuthController` | Không rate-limit / lockout khi đăng nhập sai liên tiếp — dễ bị brute-force mật khẩu | Thêm rate limiting + khóa tài khoản tạm thời sau N lần sai | Mở |
| S-05 | **Info** | `Program.cs` | Demo không bật HTTPS redirect; chấp nhận cho môi trường local | Production bắt buộc HTTPS (reverse proxy + `UseHttpsRedirection`) | Chấp nhận (demo) |

## 3. Các control đã triển khai tốt

- **Mật khẩu** lưu dạng BCrypt hash (`User.PasswordHash`), không bao giờ trả hash/secret trong response/log.
- **JWT** ký HS256, validate `issuer`/`audience`/`exp`/`signing key`, clock skew 1 phút; `MapInboundClaims=false` để claim `role` nhất quán.
- **Default deny** + **RBAC**: endpoint nhạy cảm đều gắn `[Authorize(Roles=...)]`; chỉ `login`/`refresh` là `[AllowAnonymous]`.
- **Chống IDOR**: `MeService` luôn lấy `tenantId` từ `Tenant.UserId == userId` rồi lọc dữ liệu, không dùng id do client gửi → truy cập dữ liệu người khác trả 403/404.
- **SQL injection**: truy vấn qua LINQ parameterized; không có SQL ghép chuỗi từ dữ liệu người dùng.
- **Nhất quán dữ liệu**: tạo hóa đơn/ghi thanh toán nằm trong transaction; unique index trên `username`, `room name`, `contract_code`, `invoice_code`.

## 4. Kiểm thử bảo mật (đã có / khuyến nghị)

| Kịch bản | Kết quả kỳ vọng | Trạng thái |
| --- | --- | --- |
| Gọi protected API không token / token hết hạn | 401 | Có (test 401 — AC-05) |
| Tenant truy cập hợp đồng/hóa đơn của tenant khác | 403/404 | Có (test 403 — AC-06) |
| Tài khoản thiếu vai trò gọi API quản trị | 403 | Có (`[Authorize(Roles=...)]`) |
| Secret/PII xuất hiện trong response/log | 0 lần | Khuyến nghị scan tự động |

## 5. Lộ trình khắc phục

1. **Release này**: bỏ fallback secret (S-02), thu hẹp CORS bằng allowlist (S-03) — thay đổi nhỏ, rủi ro thấp.
2. **Release sau**: chuyển secret sang env/secrets manager (S-01), thêm rate-limit + lockout (S-04), bật HTTPS (S-05).
