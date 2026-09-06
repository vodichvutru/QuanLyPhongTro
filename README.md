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
| 🔢 **Điện nước (US-005)** | Ghi chỉ số công tơ theo phòng (chặn chỉ số giảm/ngày lùi) |
| 🧾 **Hóa đơn (US-006)** | **Tự động lập hóa đơn** theo kỳ `yyyy-MM`: tiền phòng + điện (kWh) + nước (m³) + phụ phí; truy thu công nợ cũ |
| 💵 **Thanh toán (US-007)** | Ghi nhận thu tiền, tự cập nhật PartiallyPaid/Paid, chặn trả vượt |
| 📱 **Tenant portal (US-008)** | Người thuê tự tra cứu hợp đồng & hóa đơn của chính mình |
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
| `admin`  | Admin  | Quản lý user/role |
| `chutro` | Owner  | Quản lý phòng, hợp đồng, hóa đơn, thu tiền |
| `tenant1`| Tenant | Xem hợp đồng & hóa đơn của mình (đã liên kết người thuê) |

### Luồng demo gợi ý (trên giao diện web http://localhost:5255)
1. Đăng nhập `chutro`/`123456` (Chủ trọ) → **Dashboard** xem số phòng, doanh thu, công nợ.
2. **Phòng trọ** → Thêm phòng · **Người thuê** → Thêm người thuê (seed đã có 3 phòng & 1 người thuê).
3. **Hợp đồng** → Lập hợp đồng cho phòng trống.
4. **Chỉ số điện nước** → chọn phòng → ghi chỉ số đầu kỳ rồi cuối kỳ.
5. **Hóa đơn** → "Lập hóa đơn" (chọn phòng + kỳ) → tự tính tiền phòng/điện/nước → **Xem & thu** để ghi nhận thanh toán.
6. **Báo cáo** → xem biểu đồ doanh thu, công nợ, xuất CSV.
7. Đăng nhập `tenant1`/`123456` → xem **Hóa đơn của tôi**, gửi **Yêu cầu sửa chữa**.
8. Đăng nhập `admin`/`123456` → quản lý **Người dùng** & vai trò.

Hoặc dùng trực tiếp qua Swagger nếu thích thao tác API.

---

## 🔗 API chính (chi tiết trong Swagger)

| Endpoint | Quyền | Mô tả |
|----------|-------|-------|
| `POST /api/auth/login` · `refresh` | Public | Đăng nhập / làm mới token |
| `GET /api/auth/me` | Đã đăng nhập | Thông tin tài khoản |
| `GET/POST/PUT /api/admin/users`, `PUT .../roles`, `GET /api/admin/roles` | Admin | Quản lý user/role |
| `GET/POST/PUT/DELETE /api/rooms` | Admin, Owner | Quản lý phòng |
| `GET/POST/PUT/DELETE /api/tenants` | Admin, Owner | Quản lý người thuê |
| `GET/POST /api/contracts`, `POST /{id}/terminate` | Admin, Owner | Quản lý hợp đồng |
| `POST /api/meter-readings`, `GET .../room/{roomId}` | Admin, Owner | Ghi chỉ số điện nước |
| `POST /api/invoices`, `GET`, `POST /{id}/cancel` | Admin, Owner | Lập/xem/hủy hóa đơn |
| `GET/POST /api/payments`, `GET /{id}`, `GET /invoice/{id}`, `GET /recent` | Admin, Owner | Thu tiền & lịch sử |
| `GET/POST /api/repair-requests`, `PUT /{id}/status`, `DELETE /{id}` | Admin, Owner | Quản lý yêu cầu sửa chữa |
| `GET /api/reports/dashboard` · `revenue` · `debt` · `debt-by-room` · `revenue/export` | Admin, Owner | Thống kê & xuất CSV |
| `GET /api/me/contracts` · `invoices` · `payments` | Tenant | Cổng tra cứu người thuê |
| `GET/POST /api/me/repair-requests`, `POST /{id}/cancel` | Tenant | Người thuê gửi/hủy yêu cầu sửa chữa |

---

## 🛠️ Công nghệ

- **.NET 8** / ASP.NET Core Web API / EF Core 8 (Pomelo MySQL 8.0.x)
- **JWT Bearer** (access 24h + refresh), **BCrypt.Net** hash mật khẩu
- **xUnit** unit tests · **Swagger/OpenAPI**
