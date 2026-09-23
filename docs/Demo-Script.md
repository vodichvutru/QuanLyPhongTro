# Kịch bản Demo & Phản biện — Quản Lý Phòng Trọ

Tài liệu hỗ trợ buổi bảo vệ (tiêu chí **Demo & làm việc nhóm**). Đề xuất tổng thời lượng 10–12 phút demo + 5 phút Q&A.

---

## 0. Chuẩn bị

- Chạy app: `docker compose up --build` (hoặc `dotnet run`). Mở http://localhost:8080.
- Đăng nhập sẵn 3 tài khoản ở 3 tab riêng: `admin`, `chutro`, `nguyenvana` (mật khẩu `123456`).
- Đảm bảo DB đã seed: ~10 phòng, 7 người thuê, hợp đồng, ~30 hóa đơn trải 5 tháng, thanh toán, 5 yêu cầu sửa chữa.

## 1. Mở đầu (1 phút) — Nguyễn Tiến Dũng

- **Vấn đề**: chủ trọ quản lý sổ sách/bảng tính rời rạc → sai sót, khó tra cứu, mất thời gian chốt tiền.
- **Giải pháp**: một hệ thống web tập trung quản lý toàn vòng đời `phòng → người thuê → hợp đồng → điện nước → hóa đơn → thanh toán`, kèm cổng tự phục vụ cho người thuê.
- **Kiến trúc**: ASP.NET Core 8 Web API (modular monolith) + MySQL 8, JWT/RBAC, frontend SPA. → chiếu sơ đồ kiến trúc (mục 6 tài liệu).

## 2. Đăng nhập & phân quyền (1.5 phút) — Trần Phong

| Màn hình | Thao tác | Điểm cần nói |
| --- | --- | --- |
| Login | Đăng nhập `chutro` | JWT được cấp (access 24h + refresh 7 ngày, HS256) |
| Login | Thử đăng nhập sai mật khẩu | Trả 401, không lộ thông tin tài khoản |
| Trang chủ | Chuyển sang tab `nguyenvana` | Người thuê chỉ thấy dữ liệu của mình (tenant portal) |
| Trang chủ | Tab `admin` | Admin thấy màn hình quản trị user/role |

**Nói**: "Mọi API đều default deny — chỉ login/refresh là public. Phân quyền theo 3 vai trò Admin/Chủ trọ/Người thuê; người thuê chỉ truy cập được dữ liệu của chính mình vì `tenant_id` được suy ra từ userId trong JWT ở server, không tin id từ client."

## 3. Nghiệp vụ chủ trọ (3 phút) — Trần Phong

1. **Phòng**: tạo phòng mới, đổi trạng thái (Trống → Đang thuê → Bảo trì).
2. **Người thuê**: tạo hồ sơ + tạo tài khoản đăng nhập (role Tenant) gắn với hồ sơ.
3. **Hợp đồng**: tạo hợp đồng (phòng + người thuê + giá thuê + đơn giá điện/nước + cọc). Nói: "unique contract_code; không cho hợp đồng hiệu lực chồng lấn."
4. **Chỉ số điện nước**: ghi chỉ số → hệ thống ước tính tiền theo hợp đồng; từ chối nếu chỉ số mới < cũ.
5. **Hóa đơn**: chọn phòng + kỳ → **Preview** trước khi lưu → lưu hóa đơn. Nói: "tính tiền phòng + điện + nước + phụ thu, chống tạo trùng kỳ (409)."
6. **Thanh toán**: ghi nhận thanh toán → trạng thái hóa đơn chuyển Unpaid → Partial → Paid.

## 4. Cổng người thuê (1.5 phút) — Nguyễn Ngọc Tuấn

1. Người thuê xem hợp đồng + danh sách hóa đơn của mình.
2. **Thanh toán online** hóa đơn (chọn hình thức chuyển khoản/MoMo/VNPay) → trạng thái cập nhật.
3. Gửi yêu cầu sửa chữa → chủ trọ duyệt/chuyển trạng thái → người thuê theo dõi.

## 5. Admin & báo cáo (1 phút) — Nguyễn Tiến Dũng

1. Admin: danh sách user, gán vai trò, khóa/kích hoạt tài khoản, đặt lại mật khẩu.
2. Chủ trọ: Dashboard (số phòng, doanh thu tháng, công nợ), báo cáo doanh thu theo tháng, công nợ theo phòng.

## 6. Điểm nhấn kỹ thuật (1 phút) — Trần Phong

- **Bảo mật**: BCrypt, JWT validate issuer/audience/expiry, default deny + RBAC, chống IDOR — chiếu threat model (6.7) + `docs/Security-Review.md`.
- **Nhất quán**: transaction khi tạo hóa đơn/thanh toán; unique constraint chống trùng.
- **Hạ tầng**: Docker + docker-compose (MySQL + API), healthcheck.
- **Chất lượng**: 38/38 test pass; load-test k6 (`tests/load/k6-loadtest.js`) với ngưỡng p95 ≤ 500ms.

---

## Phản biện dự kiến (Q&A)

| Câu hỏi | Trả lời |
| --- | --- |
| Vì sao chỉ lấy `tenant_id` từ token là đủ chống truy cập chéo? | Token **không chứa** `tenant_id` — chỉ có `userId` (claim `nameid`) và `role`. Server suy `tenantId` từ `Tenant.UserId == userId` rồi lọc mọi truy vấn theo `tenant_id`; không bao giờ tin id do client gửi. Chứng minh: gọi `/api/me/invoices/{id}` với id hóa đơn của tenant khác → trả 404/403 (test AC-06). |
| Tại sao chọn role-based chứ không permission fine-grained? | ADR-002: 3 vai trò đủ đáp ứng BR-07/08 với ít thành phần; permission `resource.action` + bảng `role_permission` là lộ trình khi cần phân quyền mịn. |
| Làm sao chống tạo trùng hóa đơn cùng kỳ? | Kiểm tra ở service + unique constraint + transaction; request trùng trả 409. |
| Đảm bảo số tiền hóa đơn đúng? | `InvoiceCalculator` tính từ hợp đồng + chỉ số, có unit test; preview trước khi lưu; transaction rollback nếu lỗi. |
| Secret lộ trong `appsettings.json`? | Đã ghi nhận trong `docs/Security-Review.md` (finding S-01) và khuyến nghị chuyển sang env/secret manager; Docker compose đã dùng biến môi trường. |
| Hệ thống scale thế nào nếu nhiều phòng? | Modular monolith dễ mở; index theo query thật; khi cần scale độc lập → tách service (ADR-001). |
| Vì sao dùng HS256 mà không RS256? | ADR-003: một dịch vụ, HS256 đơn giản; chuyển RS256 + revocation khi triển khai đa dịch vụ. |

## Phân công

| Vai trò | Thành viên | Phần |
| --- | --- | --- |
| BA / Product | Nguyễn Tiến Dũng | Mở đầu, báo cáo, Q&A nghiệp vụ |
| Architect / Backend | Trần Phong | Kiến trúc, đăng nhập/phân quyền, nghiệp vụ chủ trọ, kỹ thuật |
| Security / Data | Nguyễn Ngọc Tuấn | Cổng người thuê, bảo mật, dữ liệu |
