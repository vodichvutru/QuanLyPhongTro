# Quản Lý Phòng Trọ & Thanh Toán Tiền Thuê

Hệ thống quản lý phòng trọ và thanh toán tiền thuê — đồ án **Lập trình Web nâng cao** (Nhóm 16).

Backend **ASP.NET Core 8 Web API** (modular monolith) + **MySQL 8**, phân quyền **JWT/RBAC**, kèm frontend SPA tĩnh (wwwroot). Toàn bộ vòng đời: **phòng → người thuê → hợp đồng → điện/nước → hóa đơn → thanh toán** và **yêu cầu sửa chữa**.

---

## Tính năng chính

| Nhóm | Tính năng |
| --- | --- |
| Xác thực & phân quyền | Đăng nhập/refresh JWT, 3 vai trò Admin / Chủ trọ (Owner) / Người thuê (Tenant) |
| Phòng & người thuê | Quản lý phòng, hồ sơ người thuê, liên kết tài khoản |
| Hợp đồng | Tạo/kết thúc hợp đồng, đơn giá điện/nước, đặt cọc |
| Chỉ số điện nước | Ghi chỉ số, ước tính tiền theo hợp đồng |
| Hóa đơn | Tạo hóa đơn hàng tháng, dự toán trước khi lưu, chống trùng kỳ |
| Thanh toán | Ghi nhận thanh toán (tiền mặt/chuyển khoản/MoMo/VNPay), người thuê tự thanh toán online hóa đơn của mình |
| Yêu cầu sửa chữa | Người thuê gửi, chủ trọ duyệt/xử lý |
| Báo cáo | Dashboard doanh thu, công nợ theo phòng |

## Kiến trúc

**Modular monolith** chia 3 tầng + API (xem ADR-001 trong tài liệu thiết kế):

```
src/
├── QuanLyPhongTro.Api/           # Controllers, JWT, middleware, wwwroot (SPA)
├── QuanLyPhongTro.Application/    # Services nghiệp vụ, DTOs, security interface
├── QuanLyPhongTro.Core/           # Entities, Enums (domain)
└── QuanLyPhongTro.Infrastructure/ # EF Core (MySQL), DbContext, Seeder
```

Các module nghiệp vụ: **Identity/RBAC**, **Room Rental**, **Billing & Payment**, **Maintenance**.

## Công nghệ

- **.NET 8 / ASP.NET Core Web API** + **EF Core (Pomelo MySQL 8)**
- **JWT Bearer** (HS256) + RBAC theo vai trò; **BCrypt** hash mật khẩu
- **Swagger/OpenAPI**, frontend SPA tĩnh (HTML/CSS/JS)
- **xUnit** (EF Core InMemory) — 38 test
- **Docker + docker-compose**

## Yêu cầu

- .NET 8 SDK
- MySQL 8 (nếu chạy local, không dùng Docker)

## Chạy nhanh

### 1) Docker (khuyến nghị)

```bash
docker compose up --build
```

- API: http://localhost:8080 (Swagger tại `/swagger` khi chạy Development)
- MySQL: `localhost:3306` (user `root`, password `123456` mặc định — đổi qua biến `MYSQL_ROOT_PASSWORD`)
- Database `quanlyphongtro` được tự tạo schema + seed dữ liệu demo ở lần khởi động đầu.

### 2) Chạy local

```bash
# đảm bảo MySQL đang chạy, connection string trong appsettings.json
dotnet run --project src/QuanLyPhongTro.Api
```

App tự tạo DB + seed khi chạy. Muốn reset về bộ demo: đặt `Seed:Reset=true` (chỉ lần đó).

### Tài khoản seed (password đều `123456`)

| Vai trò | Username |
| --- | --- |
| Admin | `admin` |
| Chủ trọ | `chutro` |
| Người thuê | `nguyenvana`, `tranthibinh`, `levancuong`, … |

## API chính

| Phương thức | Endpoint | Quyền |
| --- | --- | --- |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/refresh` | Public |
| GET/POST/PUT | `/api/rooms` | Admin, Owner |
| GET/POST | `/api/contracts`, `/api/invoices`, `/api/payments` | Admin, Owner |
| POST | `/api/meter-readings` | Admin, Owner |
| GET/POST | `/api/me/invoices`, `/api/me/repair-requests` | Tenant (scope theo bản thân) |
| POST | `/api/me/invoices/{id}/pay` | Tenant (thanh toán hóa đơn của mình) |
| GET/POST | `/api/admin/*` | Admin |

## Bảo mật

- **Default deny**: mọi controller yêu cầu `[Authorize]`; chỉ `login`/`refresh` là `[AllowAnonymous]`.
- **RBAC theo vai trò** (`[Authorize(Roles=...)]`) + scope dữ liệu người thuê ở server (`tenant_id` từ `UserId` trong JWT, không tin client).
- JWT HS256 (access 24h + refresh 7 ngày), validate issuer/audience/lifetime; mật khẩu hash BCrypt.
- Chi tiết threat model & pentest: xem [docs/Security-Review.md](docs/Security-Review.md).

## Kiểm thử

```bash
dotnet test            # 38 test đơn vị/tích hợp
k6 run tests/load/k6-loadtest.js   # load test p95 ≤ 500ms (cần cài k6)
```

38 test đơn vị/tích hợp (Auth, Invoice tính toán, MeService ownership, Payment, RepairRequest, tài khoản người thuê). Load test k6 kiểm chứng NFR-PERF-01.

## Tài liệu

- [Quan_Ly_Phong_Tro.md](Quan_Ly_Phong_Tro.md) — Phân tích & thiết kế hệ thống (Use Case, User Story, FR/NFR, DB, System Design, ADR, traceability)
- [docs/Database-Schema.md](docs/Database-Schema.md), [docs/Deployment-Guide.md](docs/Deployment-Guide.md), [docs/Security-Review.md](docs/Security-Review.md)
- [docs/Demo-Script.md](docs/Demo-Script.md) — kịch bản demo & phản biện bảo vệ
- [Sprints/](Sprints/) — kế hoạch Sprint 0–4
