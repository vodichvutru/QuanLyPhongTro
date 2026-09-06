# Sprint 1: Room Management

**Thời gian:** Tuần 2-3  
**Mục tiêu:** CRUD phòng trọ và người thuê

---

## 🎯 Sprint Goal

Hoàn thành chức năng quản lý phòng trọ và người thuê cho chủ trọ (Owner), bao gồm:
- Quản lý phòng (tạo, sửa, xóa, thay đổi trạng thái)
- Quản lý người thuê (tạo, sửa, liên kết với phòng)

---

## 📋 User Stories

### US-002: Quản lý phòng trọ

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn quản lý phòng, để biết tình trạng và giá thuê từng phòng.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể tạo phòng mới với số phòng, giá thuê, mô tả
- [ ] AC-02: Owner có thể cập nhật thông tin phòng
- [ ] AC-03: Owner có thể thay đổi trạng thái phòng (Trống/Đã thuê/Bảo trì)
- [ ] AC-04: Owner có thể xóa phòng (chỉ phòng trống)
- [ ] AC-05: System ngăn tạo 2 phòng trùng số phòng (BR-02)
- [ ] AC-06: Non-owner bị từ chối truy cập (403)

**Business Rules:**
- BR-02: Số phòng (room_number) phải duy nhất trong hệ thống
- BR-03: Giá thuê (rent_price) > 0
- Room statuses: `Available`, `Rented`, `Maintenance`

---

### US-003: Quản lý người thuê

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn quản lý người thuê, để liên kết người thuê với hợp đồng và phòng.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể tạo hồ sơ người thuê (tên, CCCD, SĐT, email)
- [ ] AC-02: Owner có thể cập nhật thông tin người thuê
- [ ] AC-03: Owner có thể xem danh sách người thuê
- [ ] AC-04: Owner có thể tạo tài khoản cho người thuê (Tenant role)
- [ ] AC-05: Non-owner bị từ chối truy cập (403)

**Business Rules:**
- BR-01: Email/username là duy nhất
- CCCD format validation

---

## 🛠️ Technical Tasks

### 1. Room Entity & Repository

```csharp
// Entities/Room.cs
public class Room
{
    public long Id { get; set; }
    public string RoomNumber { get; set; }      // Unique
    public decimal RentPrice { get; set; }      // > 0
    public string Description { get; set; }
    public RoomStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Contract> Contracts { get; set; }
    public ICollection<MeterReading> MeterReadings { get; set; }
}

public enum RoomStatus
{
    Available = 1,
    Rented = 2,
    Maintenance = 3
}
```

### 2. Tenant Entity & Repository

```csharp
// Entities/Tenant.cs
public class Tenant
{
    public long Id { get; set; }
    public string FullName { get; set; }
    public string IdentityNumber { get; set; }    // CCCD
    public string Phone { get; set; }
    public string Email { get; set; }
    public long? UserId { get; set; }          // Nullable - linked account
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public User User { get; set; }
    public ICollection<Contract> Contracts { get; set; }
}
```

### 3. DTOs

**Room DTOs:**
```csharp
// CreateRoomRequest
{
    "roomNumber": "101",
    "rentPrice": 5000000,
    "description": "Phòng trọ tầng 1"
}

// UpdateRoomRequest
{
    "rentPrice": 5500000,
    "description": "Phòng mới reno",
    "status": "Rented"  // Optional
}

// RoomResponse
{
    "id": 1,
    "roomNumber": "101",
    "rentPrice": 5000000,
    "description": "Phòng trọ tầng 1",
    "status": "Available",
    "createdAt": "2026-09-01T00:00:00Z"
}
```

**Tenant DTOs:**
```csharp
// CreateTenantRequest
{
    "fullName": "Nguyễn Văn A",
    "identityNumber": "012345678901",
    "phone": "0901234567",
    "email": "nguyenvana@email.com",
    "createAccount": true  // Tạo tài khoản cho tenant
}

// UpdateTenantRequest
{
    "fullName": "Nguyễn Văn B",
    "phone": "0912345678"
}

// TenantResponse
{
    "id": 1,
    "fullName": "Nguyễn Văn A",
    "identityNumber": "012345678901",
    "phone": "0901234567",
    "email": "nguyenvana@email.com",
    "hasAccount": true,
    "createdAt": "2026-09-01T00:00:00Z"
}
```

### 4. Services

- [ ] RoomService
  - `GetAllAsync(filter, pagination)` - Lấy danh sách phòng
  - `GetByIdAsync(id)` - Lấy chi tiết phòng
  - `CreateAsync(request)` - Tạo phòng mới
  - `UpdateAsync(id, request)` - Cập nhật phòng
  - `UpdateStatusAsync(id, status)` - Cập nhật trạng thái
  - `DeleteAsync(id)` - Xóa phòng

- [ ] TenantService
  - `GetAllAsync(filter, pagination)`
  - `GetByIdAsync(id)`
  - `CreateAsync(request)`
  - `UpdateAsync(id, request)`
  - `CreateWithAccountAsync(request)` - Tạo tenant + account

### 5. Validations

- [ ] Room number unique check
- [ ] Rent price > 0
- [ ] Identity number format (12 digits)
- [ ] Phone format (10-11 digits)
- [ ] Email format validation

---

## 📡 API Endpoints

### Rooms Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/rooms | Yes | room.read | Danh sách phòng (filter: status) |
| GET | /api/rooms/{id} | Yes | room.read | Chi tiết phòng |
| POST | /api/rooms | Yes | room.create | Tạo phòng mới |
| PUT | /api/rooms/{id} | Yes | room.update | Cập nhật phòng |
| PATCH | /api/rooms/{id}/status | Yes | room.update | Cập nhật trạng thái |
| DELETE | /api/rooms/{id} | Yes | room.delete | Xóa phòng |

**Query Parameters:**
```
GET /api/rooms?status=Available&page=1&size=20&sort=roomNumber&order=asc
```

**Response:**
```json
{
    "data": [...],
    "pagination": {
        "page": 1,
        "size": 20,
        "totalItems": 100,
        "totalPages": 5
    }
}
```

### Tenants Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/tenants | Yes | tenant.read | Danh sách người thuê |
| GET | /api/tenants/{id} | Yes | tenant.read | Chi tiết người thuê |
| POST | /api/tenants | Yes | tenant.create | Tạo người thuê |
| PUT | /api/tenants/{id} | Yes | tenant.update | Cập nhật người thuê |
| DELETE | /api/tenants/{id} | Yes | tenant.delete | Xóa người thuê |

---

## ✅ Definition of Done

- [ ] CRUD phòng hoạt động đúng spec
- [ ] CRUD người thuê hoạt động đúng spec
- [ ] Authorization đúng: chỉ Owner mới được truy cập
- [ ] Validation messages rõ ràng
- [ ] Pagination hoạt động đúng
- [ ] Filter theo status hoạt động
- [ ] Unit tests cho RoomService
- [ ] Unit tests cho TenantService
- [ ] Swagger documentation đầy đủ

---

## 📊 Metrics

| Metric | Target |
|--------|--------|
| Velocity | - |
| Tasks Completed | - |
| Code Coverage | >60% |
| Bug Count | 0 |

---

## 🔗 Dependencies

- Sprint 0 completed (Auth & RBAC)
- Database migrations ready

---

## 📝 Notes

- Phòng có Contract đang active không được xóa
- Khi tạo account cho Tenant: password mặc định là CCCD (yêu cầu đổi khi login lần đầu)
- RoomNumber format: "Số tầng" + "Số phòng" (VD: "101", "2A5")
