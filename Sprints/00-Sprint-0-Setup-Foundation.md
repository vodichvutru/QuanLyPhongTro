# Sprint 0: Setup & Foundation

**Thời gian:** Tuần 1  
**Mục tiêu:** Kiến trúc, database, authentication core

---

## 🎯 Sprint Goal

Thiết lập nền tảng kỹ thuật cho toàn bộ dự án: project structure, database, JWT authentication, và RBAC cơ bản.

---

## 📋 User Stories

### US-001: Login & JWT Authentication

**Actor:** Tất cả user  
**Mô tả:** Là user, tôi muốn đăng nhập để nhận JWT hợp lệ, để sử dụng đúng chức năng được cấp.

**Acceptance Criteria:**
- [ ] AC-01: User đăng nhập với username/password đúng → nhận JWT
- [ ] AC-02: User đăng nhập với username/password sai → nhận lỗi 401
- [ ] AC-03: JWT chứa thông tin userId, username, roles

**Technical Details:**
- JWT expiry: 24h
- Claims: `sub` (userId), `username`, `roles[]`
- Password hash: bcrypt

---

### US-010: RBAC - Quản lý User/Role/Permission

**Actor:** Quản trị viên  
**Mô tả:** Là quản trị viên, tôi muốn gán role/permission cho user, để kiểm soát quyền truy cập nhất quán.

**Acceptance Criteria:**
- [ ] AC-01: Admin có thể tạo/sửa/xóa user
- [ ] AC-02: Admin có thể gán role cho user
- [ ] AC-03: User không có quyền admin bị từ chối (403) khi truy cập endpoint admin

**Technical Details:**
- Roles: `Admin`, `Owner`, `Tenant`
- Permissions format: `resource.action` (VD: `room.read`, `invoice.create`)

---

## 🛠️ Technical Tasks

### 1. Project Structure
```
QuanLyPhongTro/
├── src/
│   ├── QuanLyPhongTro.Api/           # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Program.cs
│   ├── QuanLyPhongTro.Core/         # Domain entities, interfaces
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Interfaces/
│   ├── QuanLyPhongTro.Infrastructure/ # EF Core, repositories
│   │   ├── Data/
│   │   ├── Repositories/
│   │   └── Services/
│   └── QuanLyPhongTro.Application/   # Use cases, DTOs
│       ├── DTOs/
│       ├── Services/
│       └── Validators/
├── tests/
│   └── QuanLyPhongTro.Tests/
├── docs/
└── QuanLyPhongTro.sln
```

### 2. Database Design

**Entities cần tạo:**
- [ ] Users (username, password_hash, email, status)
- [ ] Roles (name, description)
- [ ] Permissions (code, name)
- [ ] UserRoles (user_id, role_id)
- [ ] RolePermissions (role_id, permission_id)

**Permissions seed data:**
```json
[
  "auth.login", "auth.refresh",
  "user.read", "user.create", "user.update", "user.delete",
  "role.read", "role.create", "role.update", "role.delete",
  "room.read", "room.create", "room.update", "room.delete",
  "tenant.read", "tenant.create", "tenant.update", "tenant.delete",
  "contract.read", "contract.create", "contract.update", "contract.delete",
  "meter.read", "meter.create", "meter.update",
  "invoice.read", "invoice.create", "invoice.update",
  "payment.read", "payment.create",
  "repair.read", "repair.create", "repair.update",
  "invoice.read.own", "repair.create.own"
]
```

### 3. Authentication Implementation

**Auth Endpoints:**
- [ ] `POST /api/auth/login` - Đăng nhập
- [ ] `POST /api/auth/refresh` - Refresh token
- [ ] `GET /api/auth/me` - Lấy thông tin user hiện tại

**JWT Configuration:**
```yaml
JWT:
  SecretKey: "your-256-bit-secret-key-here"
  Issuer: "QuanLyPhongTro"
  Audience: "QuanLyPhongTro"
  ExpiryHours: 24
```

### 4. Authorization Middleware

- [ ] JwtMiddleware - Extract và validate JWT
- [ ] PermissionMiddleware - Check permission trước khi execute controller
- [ ] Global exception handler

---

## 📡 API Endpoints

### Auth Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| POST | /api/auth/login | No | - | Đăng nhập |
| POST | /api/auth/refresh | No | - | Refresh token |
| GET | /api/auth/me | Yes | - | Lấy profile |

### Admin Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/admin/users | Yes | user.read | Danh sách users |
| GET | /api/admin/users/{id} | Yes | user.read | Chi tiết user |
| POST | /api/admin/users | Yes | user.create | Tạo user |
| PUT | /api/admin/users/{id} | Yes | user.update | Cập nhật user |
| DELETE | /api/admin/users/{id} | Yes | user.delete | Xóa user |
| GET | /api/admin/roles | Yes | role.read | Danh sách roles |
| POST | /api/admin/roles | Yes | role.create | Tạo role |
| PUT | /api/admin/roles/{id} | Yes | role.update | Cập nhật role |
| POST | /api/admin/roles/{roleId}/permissions | Yes | role.update | Gán permissions |
| GET | /api/admin/permissions | Yes | role.read | Danh sách permissions |

---

## ✅ Definition of Done

- [ ] Project chạy được với `dotnet run`
- [ ] Database migrations apply thành công
- [ ] API `/api/auth/login` trả JWT hợp lệ
- [ ] Protected endpoints trả 401 khi không có JWT
- [ ] Protected endpoints trả 403 khi thiếu permission
- [ ] Unit tests cho authentication logic
- [ ] Swagger documentation đầy đủ

---

## 📊 Metrics

| Metric | Target |
|--------|--------|
| Velocity | - |
| Burndown | - |
| Code Coverage | >60% |
| API Tests | >80% passed |

---

## 🔗 Dependencies

- .NET 8 SDK
- MySQL 8.0
- Entity Framework Core 8.x
- JWT Bearer Package

---

## 📝 Notes

- Seed data: tạo admin user mặc định (admin/admin123)
- Database name: `quanlyphongtro`
- Connection string config trong `appsettings.json`
