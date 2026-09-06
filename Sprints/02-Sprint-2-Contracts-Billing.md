# Sprint 2: Contracts & Billing

**Thời gian:** Tuần 4-5  
**Mục tiêu:** Hợp đồng, chỉ số điện nước, tạo hóa đơn tự động

---

## 🎯 Sprint Goal

Hoàn thành module hợp đồng và billing:
- Quản lý hợp đồng thuê phòng
- Ghi nhận chỉ số điện nước hàng tháng
- Tạo hóa đơn tự động từ hợp đồng + điện nước

---

## 📋 User Stories

### US-004: Quản lý hợp đồng thuê

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn quản lý hợp đồng, để kiểm soát thời hạn và giá thuê.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể tạo hợp đồng mới cho phòng
- [ ] AC-02: Owner có thể xem chi tiết hợp đồng
- [ ] AC-03: Owner có thể cập nhật thông tin hợp đồng
- [ ] AC-04: Owner có thể kết thúc hợp đồng sớm
- [ ] AC-05: Hợp đồng mới không được chồng lấn thời gian với hợp đồng hiện có
- [ ] AC-06: Khi tạo hợp đồng mới → tự động cập nhật trạng thái phòng sang "Đã thuê"
- [ ] AC-07: Khi hợp đồng kết thúc → tự động cập nhật trạng thái phòng sang "Trống"

**Business Rules:**
- BR-02: Một phòng không được có hai hợp đồng đang hiệu lực chồng lấn cùng thời gian
- BR-03: Ngày kết thúc >= Ngày bắt đầu
- BR-04: Giá thuê trong hợp đồng có thể khác giá phòng hiện tại (để theo dõi lịch sử)

**Contract Lifecycle:**
```
Draft → Active → Expired
         ↓
     Terminated (early end)
```

---

### US-005: Ghi nhận chỉ số điện nước

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn nhập chỉ số điện nước, để tính đúng mức tiêu thụ.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể nhập chỉ số điện mới cho phòng
- [ ] AC-02: Owner có thể nhập chỉ số nước mới cho phòng
- [ ] AC-03: Owner có thể xem lịch sử chỉ số điện nước của phòng
- [ ] AC-04: Chỉ số mới phải >= chỉ số cũ của cùng kỳ
- [ ] AC-05: Không cho phép nhập 2 chỉ số cho cùng 1 phòng + cùng 1 kỳ

**Business Rules:**
- BR-03: Chỉ số mới phải >= chỉ số cũ (cùng loại: điện hoặc nước)
- BR-05: Billing period format: YYYY-MM

**Billing Period:**
- Mặc định: theo tháng
- Owner có thể cấu hình ngày chốt (1-28)

---

### US-006: Tạo hóa đơn tiền thuê

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn tạo hóa đơn từ hợp đồng và chỉ số điện nước, để giảm tính toán thủ công.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể tạo hóa đơn cho một phòng trong một kỳ
- [ ] AC-02: Hóa đơn tự động tính: Tiền phòng + Tiền điện + Tiền nước + Phụ thu
- [ ] AC-03: Không tạo trùng hóa đơn cho cùng phòng + kỳ (trả 409)
- [ ] AC-04: Hóa đơn được tạo với trạng thái "Chưa thanh toán"
- [ ] AC-05: Tiền điện = (chỉ số điện mới - chỉ số điện cũ) × đơn giá điện
- [ ] AC-06: Tiền nước = (chỉ số nước mới - chỉ số nước cũ) × đơn giá nước

**Business Rules:**
- BR-04: Mỗi phòng chỉ có tối đa một hóa đơn cho một kỳ hóa đơn
- BR-05: Tổng tiền hóa đơn = tiền phòng + tiền điện + tiền nước + phụ thu - giảm trừ

**Invoice Lifecycle:**
```
Draft → Unpaid → PartiallyPaid → Paid
          ↓
       Overdue
          ↓
      Cancelled
```

**Invoice Items:**
- Rent (tiền phòng)
- Electricity (tiền điện)
- Water (tiền nước)
- Other (phụ thu/giảm trừ)

---

## 🛠️ Technical Tasks

### 1. Contract Entity

```csharp
public class Contract
{
    public long Id { get; set; }
    public long RoomId { get; set; }
    public long TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentPrice { get; set; }    // Giá thuê trong hợp đồng
    public decimal DepositAmount { get; set; }
    public ContractStatus Status { get; set; }
    public string Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public Room Room { get; set; }
    public Tenant Tenant { get; set; }
    public ICollection<Invoice> Invoices { get; set; }
}

public enum ContractStatus
{
    Draft = 0,
    Active = 1,
    Expired = 2,
    Terminated = 3
}
```

### 2. MeterReading Entity

```csharp
public class MeterReading
{
    public long Id { get; set; }
    public long RoomId { get; set; }
    public string BillingPeriod { get; set; }  // "2026-09"
    public decimal? ElectricityOld { get; set; }
    public decimal? ElectricityNew { get; set; }
    public decimal? WaterOld { get; set; }
    public decimal? WaterNew { get; set; }
    
    public DateTime RecordedAt { get; set; }
    public long RecordedBy { get; set; }
    
    // Navigation
    public Room Room { get; set; }
}
```

### 3. Invoice Entity

```csharp
public class Invoice
{
    public long Id { get; set; }
    public long RoomId { get; set; }
    public string BillingPeriod { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ElectricityAmount { get; set; }
    public decimal WaterAmount { get; set; }
    public decimal OtherAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    
    // Navigation
    public Room Room { get; set; }
    public ICollection<InvoiceItem> Items { get; set; }
    public ICollection<Payment> Payments { get; set; }
}

public class InvoiceItem
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public InvoiceItemType Type { get; set; }
    public string Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

public enum InvoiceItemType
{
    Rent = 1,
    Electricity = 2,
    Water = 3,
    Other = 4
}

public enum InvoiceStatus
{
    Draft = 0,
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}
```

### 4. Settings Entity (cho đơn giá)

```csharp
public class Setting
{
    public long Id { get; set; }
    public string Key { get; set; }      // "electricity_price", "water_price"
    public string Value { get; set; }
    public string Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long UpdatedBy { get; set; }
}

// Seed data:
// electricity_price = 3500 (VNĐ/kWh)
// water_price = 10000 (VNĐ/m³)
```

### 5. Billing Service

```csharp
public interface IBillingService
{
    // Meter Readings
    Task<MeterReadingDto> RecordReadingAsync(long roomId, string period, MeterReadingRequest request);
    Task<List<MeterReadingDto>> GetReadingsAsync(long roomId);
    
    // Invoices
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<InvoiceDto> GetInvoiceAsync(long invoiceId);
    Task<List<InvoiceDto>> GetInvoicesAsync(InvoiceFilterRequest filter);
    Task CancelInvoiceAsync(long invoiceId);
    
    // Calculation
    decimal CalculateElectricity(decimal oldReading, decimal newReading, decimal unitPrice);
    decimal CalculateWater(decimal oldReading, decimal newReading, decimal unitPrice);
}
```

---

## 📡 API Endpoints

### Contracts Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/contracts | Yes | contract.read | Danh sách hợp đồng |
| GET | /api/contracts/{id} | Yes | contract.read | Chi tiết hợp đồng |
| GET | /api/contracts/room/{roomId} | Yes | contract.read | Hợp đồng của phòng |
| POST | /api/contracts | Yes | contract.create | Tạo hợp đồng mới |
| PUT | /api/contracts/{id} | Yes | contract.update | Cập nhật hợp đồng |
| POST | /api/contracts/{id}/terminate | Yes | contract.update | Kết thúc hợp đồng |

**Create Contract Request:**
```json
{
    "roomId": 1,
    "tenantId": 1,
    "startDate": "2026-09-01",
    "endDate": "2027-09-01",
    "rentPrice": 5000000,
    "depositAmount": 10000000,
    "notes": "Hợp đồng thuê 1 năm"
}
```

### MeterReadings Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/meter-readings | Yes | meter.read | Danh sách chỉ số |
| GET | /api/meter-readings/room/{roomId} | Yes | meter.read | Chỉ số theo phòng |
| POST | /api/meter-readings | Yes | meter.create | Ghi nhận chỉ số |
| PUT | /api/meter-readings/{id} | Yes | meter.update | Cập nhật chỉ số |

**Record Reading Request:**
```json
{
    "roomId": 1,
    "billingPeriod": "2026-09",
    "electricityNew": 150.5,
    "waterNew": 25.0
}
```

### Invoices Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/invoices | Yes | invoice.read | Danh sách hóa đơn |
| GET | /api/invoices/{id} | Yes | invoice.read | Chi tiết hóa đơn |
| GET | /api/invoices/room/{roomId} | Yes | invoice.read | Hóa đơn theo phòng |
| POST | /api/invoices | Yes | invoice.create | Tạo hóa đơn |
| DELETE | /api/invoices/{id} | Yes | invoice.update | Hủy hóa đơn |

**Create Invoice Request:**
```json
{
    "roomId": 1,
    "billingPeriod": "2026-09",
    "otherAmount": 0,
    "dueDate": "2026-09-15"
}
```

**Invoice Response:**
```json
{
    "id": 1,
    "roomId": 1,
    "roomNumber": "101",
    "billingPeriod": "2026-09",
    "items": [
        { "type": "Rent", "description": "Tiền phòng tháng 09/2026", "amount": 5000000 },
        { "type": "Electricity", "description": "Điện: 50.5 kWh × 3500", "amount": 176750 },
        { "type": "Water", "description": "Nước: 10 m³ × 10000", "amount": 100000 }
    ],
    "totalAmount": 5276750,
    "dueDate": "2026-09-15",
    "status": "Unpaid",
    "createdAt": "2026-09-05T10:00:00Z"
}
```

### Settings Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/settings | Yes | - | Lấy cài đặt |
| PUT | /api/settings | Yes | admin | Cập nhật cài đặt |

---

## ✅ Definition of Done

- [ ] CRUD hợp đồng hoạt động đúng spec
- [ ] Validation chồng lấn hợp đồng hoạt động
- [ ] Cập nhật trạng thái phòng tự động khi tạo/kết thúc hợp đồng
- [ ] Ghi nhận chỉ số điện nước hoạt động
- [ ] Validation chỉ số mới >= chỉ số cũ
- [ ] Tạo hóa đơn tự động tính đúng
- [ ] Không tạo trùng hóa đơn (unique constraint)
- [ ] Cấu hình đơn giá điện/nước hoạt động
- [ ] Unit tests cho BillingService
- [ ] Unit tests cho Contract validation
- [ ] Integration tests cho invoice creation
- [ ] Swagger documentation đầy đủ

---

## 📊 Metrics

| Metric | Target |
|--------|--------|
| Velocity | - |
| Tasks Completed | - |
| Code Coverage | >65% |
| Bug Count | 0 |

---

## 🔗 Dependencies

- Sprint 1 completed (Rooms & Tenants)

---

## 📝 Notes

- Billing period mặc định lấy tháng hiện tại
- Due date mặc định = ngày 15 hàng tháng
- Đơn giá mặc định: Điện 3,500 VNĐ/kWh, Nước 10,000 VNĐ/m³
- Hóa đơn tạo từ hợp đồng active + meter readings của kỳ
