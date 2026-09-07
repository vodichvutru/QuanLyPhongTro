# Hệ thống Quản lý Phòng trọ & Thanh toán tiền thuê

Bài tập lớn môn **Lập trình Web nâng cao** — `.NET 8` Web API + MySQL, kiến trúc **Clean Architecture** (Api / Application / Core / Infrastructure / Tests).

> Tài liệu đặc tả đầy đủ: [`Quan_Ly_Phong_Tro.md`](Quan_Ly_Phong_Tro.md) · Kế hoạch Sprint: [`Sprints/`](Sprints/)

---

## ✅ Chức năng đã triển khai

| Nhóm | Chức năng |
|------|-----------|
| 🔐 **Auth (US-001)** | Đăng nhập JWT, refresh token, lấy thông tin tài khoản (`/me`) |
| 👥 **RBAC (US-010)** | 3 vai trò Admin/Owner/Tenant; Admin quản lý user & gán role |
| 🏠 **Phòng (US-002)** | CRUD phòng trọ, tự đổi trạng thái *Rented/Available* theo hợp đồng |
| 🧑 **Người thuê (US-003)** | CRUD hồ sơ người thuê, có thể liên kết tài khoản |
| 📄 **Hợp đồng (US-004)** | Tạo hợp đồng, tự sinh mã, kiểm tra trùng phòng/kỳ, chấm dứt hợp đồng |
| 🔢 **Điện nước (US-005)** | Ghi chỉ số công tơ theo phòng (chặn chỉ số giảm/ngày lùi); bảng ghi hiện **lượng dùng + tiền ước tính** theo giá trong hợp đồng ngay khi gõ |
| 🧾 **Hóa đơn (US-006)** | **Tự động lập hóa đơn** theo kỳ `yyyy-MM`: tiền phòng + điện (kWh × giá) + nước (m³ × giá) + phụ phí; truy thu công nợ cũ; có **bảng dự toán trước khi lưu** |
| 💵 **Thanh toán (US-007)** | Chủ trọ thu tiền (Cash/Chuyển khoản/…) tự cập nhật PartiallyPaid/Paid; **người thuê thanh toán trực tuyến** (giả lập Momo/VNPay/Chuyển khoản) ghi nhận ngay — chủ trọ xem mã giao dịch để đối chiếu |
| 📱 **Tenant portal (US-008)** | Người thuê xem hợp đồng & **hóa đơn cần trả** (điện/nước/tiền phòng), bấm **Thanh toán** để đóng trực tuyến |
| 🛠️ **Sửa chữa (US-009/010)** | Người thuê gửi yêu cầu (chỉ cho phòng đang thuê), chủ trọ tiếp nhận/xử lý, ghi phản hồi + chi phí |
| 📊 **Báo cáo (US-011)** | Dashboard, doanh thu theo tháng, công nợ (theo hóa đơn & theo phòng), xuất CSV (mở bằng Excel) |
| 🌐 **Giao diện web** | SPA không cần build (HTML/JS) — đăng nhập & dùng theo vai trò |

### Kiến trúc thư mục

```
src/
├── QuanLyPhongTro.Core/            # Entities + Enums (không phụ thuộc gì)
├── QuanLyPhongTro.Infrastructure/  # AppDbContext (EF Core + Pomelo/MySQL), DbSeeder
├── QuanLyPhongTro.Application/     # DTO, Service, logic tính hóa đơn (InvoiceCalculator)
└── QuanLyPhongTro.Api/             # Controllers, JWT, Swagger, Middleware
tests/QuanLyPhongTro.Tests/         # Unit test (xUnit) cho logic tính hóa đơn
```

---

## 🚀 Chạy dự án

### Yêu cầu
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL 8.0+ đang chạy ở `127.0.0.1:3306`

### Bước 1 — Cấu hình kết nối DB (nếu khác mặc định)
Sửa chuỗi kết nối trong `src/QuanLyPhongTro.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Server=127.0.0.1;Port=3306;Database=quanlyphongtro;User=root;Password=<DB_PASSWORD>;"
}
```

### Bước 2 — Chạy
```bash
dotnet run --project src/QuanLyPhongTro.Api
```

Khi khởi động chương trình **tự tạo database `quanlyphongtro`** (code-first `EnsureCreated`)
và **seed dữ liệu mẫu** — không cần chạy migration thủ công.

Mở trình duyệt:
- 🌐 **Giao diện web**: http://localhost:5255  (đăng nhập để dùng toàn bộ chức năng)
- 📖 **Swagger/API doc**: http://localhost:5255/swagger

> 💡 Muốn reset dữ liệu về mặc định: xóa database `quanlyphongtro` rồi chạy lại chương trình.

### Bước 3 — Test
```bash
dotnet test
```

---

## 🔑 Tài khoản dùng thử (mật khẩu mặc định: `123456`)

| Username | Vai trò | Mô tả |
|----------|---------|-------|
| `admin`  | Admin  | Quản lý người dùng & vai trò (toàn quyền) |
| `chutro` | Owner  | Chủ trọ: quản lý phòng, người thuê, hợp đồng, hóa đơn, thu tiền, sửa chữa, báo cáo |
| `nguyenvana` (Nguyễn Văn An) — tương tự `tranthibinh`, `levancuong`, `phamthidung`, `hoangvanem`, `dothiphuong`, `vuquanghoa` | Tenant | Tên đăng nhập = tên người thuê (không dấu). Mỗi người xem được **hợp đồng + hóa đơn của mình**, gửi yêu cầu sửa chữa |

> Phân quyền: **Admin** quản trị hệ thống · **Chủ trọ** quản lý kinh doanh phòng trọ · **Người thuê** chỉ xem được dữ liệu của chính mình (BR-07) — ví dụ `nguyenvana` (Nguyễn Văn An) chỉ thấy phòng P101.

**Cấp tài khoản:** chỉ Admin/Chủ trọ mới tạo được tài khoản (người thuê **không tự đăng ký**). Admin tạo tài khoản **Chủ trọ** ở trang *Người dùng*; Admin & Chủ trọ tạo tài khoản **Người thuê** gắn với hồ sơ ở trang *Người thuê* — khi "Thêm người thuê" đánh dấu *Kèm tạo tài khoản*, hoặc dùng nút *+ Tạo tài khoản* trên hồ sơ chưa có.

**Quên mật khẩu:** đặt lại hộ (không có email để gửi link). Chủ trọ đặt lại cho người thuê (nút *Đặt lại MK* ở trang Người thuê); Admin đặt lại cho mọi tài khoản ở trang *Người dùng*.

### Dữ liệu demo có sẵn
10 phòng (7 cho thuê, 3 trống) · 7 người thuê (đều có tài khoản) · 7 hợp đồng đang hiệu lực ·
25 hóa đơn trải 5 tháng (5/2026 → 9/2026) với đủ trạng thái *đã trả / trả một phần / còn nợ* ·
chỉ số điện/nước các tháng · 5 yêu cầu sửa chữa · lịch sử thanh toán.

> Đặt lại dữ liệu về đúng bộ demo khi muốn (sẽ xóa toàn bộ dữ liệu hiện có và seed lại):
> ```bash
> Seed__Reset=true dotnet run --project src/QuanLyPhongTro.Api
> ```

### Luồng demo gợi ý (trên giao diện web http://localhost:5255)
1. Đăng nhập `chutro`/`123456` (Chủ trọ) → **Dashboard** xem 10 phòng, doanh thu, công nợ.
2. **Phòng trọ** / **Người thuê** → thêm mới hoặc sửa.
3. **Hợp đồng** → lập hợp đồng cho phòng **trống** (P203/P204/P301).
4. **Hóa đơn** → "Lập hóa đơn" (phòng + kỳ) → tự tính tiền phòng/điện/nước → **Xem & thu**.
5. **Báo cáo** → biểu đồ doanh thu (5 tháng), công nợ theo phòng, xuất CSV.
6. Đăng nhập một người thuê (VD `tranthibinh`/`123456` — Trần Thị Bình, phòng P102) → **Hợp đồng của tôi**, **Hóa đơn của tôi**, **Yêu cầu sửa chữa**.
7. Đăng nhập `admin`/`123456` → quản lý **Người dùng** & vai trò.

Hoặc dùng trực tiếp qua Swagger nếu thích thao tác API.

---

## 🔗 API chính (chi tiết trong Swagger)

| Endpoint | Quyền | Mô tả |
|----------|-------|-------|
| `POST /api/auth/login` · `refresh` | Public | Đăng nhập / làm mới token |
| `GET /api/auth/me` | Đã đăng nhập | Thông tin tài khoản |
| `GET/POST/PUT /api/admin/users`, `PUT .../roles` · `.../active` · `.../password`, `GET /api/admin/roles` | Admin | Quản lý user/role, đặt lại mật khẩu |
| `GET/POST/PUT/DELETE /api/rooms` | Admin, Owner | Quản lý phòng |
| `GET/POST/PUT/DELETE /api/tenants`, `POST .../{id}/account`, `PUT .../{id}/password` | Admin, Owner | Quản lý hồ sơ & tài khoản người thuê |
| `GET/POST /api/contracts`, `POST /{id}/terminate` | Admin, Owner | Quản lý hợp đồng |
| `POST /api/meter-readings`, `GET .../room/{roomId}`, `GET .../billing-info` | Admin, Owner | Ghi chỉ số điện nước + thông tin giá/đồng hồ |
| `POST /api/invoices` · `preview`, `GET`, `POST /{id}/cancel` | Admin, Owner | Lập hóa đơn (tự tính điện/nước), xem dự toán, hủy |
| `GET/POST /api/payments`, `GET /{id}`, `GET /invoice/{id}`, `GET /recent` | Admin, Owner | Thu tiền & lịch sử |
| `GET/POST /api/repair-requests`, `PUT /{id}/status`, `DELETE /{id}` | Admin, Owner | Quản lý yêu cầu sửa chữa |
| `GET /api/reports/dashboard` · `revenue` · `debt` · `debt-by-room` · `revenue/export` | Admin, Owner | Thống kê & xuất CSV |
| `GET /api/me/contracts` · `invoices` · `payments`, `POST /api/me/invoices/{id}/pay` | Tenant | Cổng tra cứu + **thanh toán trực tuyến** của người thuê |
| `GET/POST /api/me/repair-requests`, `POST /{id}/cancel` | Tenant | Người thuê gửi/hủy yêu cầu sửa chữa |

---

## 🛠️ Công nghệ

- **.NET 8** / ASP.NET Core Web API / EF Core 8 (Pomelo MySQL 8.0.x)
- **JWT Bearer** (access 24h + refresh), **BCrypt.Net** hash mật khẩu
- **xUnit** unit tests · **Swagger/OpenAPI**
