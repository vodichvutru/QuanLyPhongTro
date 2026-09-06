# Sprint 3: Payments & Self-service

**Thời gian:** Tuần 6-7  
**Mục tiêu:** Thanh toán, tenant portal, yêu cầu sửa chữa

---

## 🎯 Sprint Goal

Hoàn thành module thanh toán và self-service:
- Ghi nhận thanh toán từ chủ trọ
- Tenant portal: xem hợp đồng, hóa đơn của mình
- Yêu cầu sửa chữa (tenant gửi, chủ trọ xử lý)

---

## 📋 User Stories

### US-007: Ghi nhận thanh toán

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn ghi nhận thanh toán, để theo dõi công nợ.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể ghi nhận thanh toán cho hóa đơn
- [ ] AC-02: Owner có thể ghi nhận thanh toán một phần
- [ ] AC-03: Khi đủ thanh toán → hóa đơn tự động chuyển sang "Đã thanh toán"
- [ ] AC-04: Không cho phép thanh toán hóa đơn đã hủy
- [ ] AC-05: Không cho phép thanh toán vượt số tiền còn lại
- [ ] AC-06: Có thể xem lịch sử thanh toán của hóa đơn

**Business Rules:**
- BR-06: Chỉ hóa đơn Chưa thanh toán/Thanh toán một phần mới được ghi nhận khoản thanh toán mới
- BR-07: Tổng thanh toán không được vượt tổng tiền hóa đơn

**Payment Lifecycle:**
```
Payment Created → Payment Confirmed
                      ↓
         Invoice Status: PartiallyPaid
                      ↓
         (more payments until fully paid)
                      ↓
         Invoice Status: Paid
```

---

### US-008: Tenant tra cứu hợp đồng/hóa đơn

**Actor:** Người thuê (Tenant)  
**Mô tả:** Là người thuê, tôi muốn xem hợp đồng và hóa đơn của mình, để chủ động theo dõi tiền thuê.

**Acceptance Criteria:**
- [ ] AC-01: Tenant có thể xem hợp đồng của mình
- [ ] AC-02: Tenant có thể xem danh sách hóa đơn của mình
- [ ] AC-03: Tenant chỉ xem được dữ liệu thuộc tài khoản của mình (BR-07)
- [ ] AC-04: Tenant xem được trạng thái thanh toán hóa đơn

**Business Rules:**
- BR-07: Người thuê chỉ được xem hợp đồng, hóa đơn thuộc tài khoản của mình

---

### US-009: Tenant gửi yêu cầu sửa chữa

**Actor:** Người thuê (Tenant)  
**Mô tả:** Là người thuê, tôi muốn gửi yêu cầu sửa chữa, để báo sự cố trực tuyến.

**Acceptance Criteria:**
- [ ] AC-01: Tenant có thể gửi yêu cầu sửa chữa cho phòng đang thuê
- [ ] AC-02: Yêu cầu có thể đính kèm hình ảnh
- [ ] AC-03: Tenant có thể xem trạng thái yêu cầu
- [ ] AC-04: Tenant chỉ gửi được yêu cầu cho phòng của mình (BR-08)

**Request Status:**
```
Pending → InProgress → Completed
             ↓
         Cancelled
```

---

### US-010: Chủ trọ xử lý yêu cầu sửa chữa

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn tiếp nhận và xử lý yêu cầu sửa chữa từ người thuê.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể xem danh sách yêu cầu sửa chữa
- [ ] AC-02: Owner có thể cập nhật trạng thái yêu cầu
- [ ] AC-03: Owner có thể ghi nhận mô tả xử lý và chi phí
- [ ] AC-04: Owner có thể hủy yêu cầu

---

## 🛠️ Technical Tasks

### 1. Payment Entity

```csharp
public class Payment
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string TransactionId { get; set; }   // Mã giao dịch ngân hàng (nếu có)
    public string Notes { get; set; }
    public DateTime PaidAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    
    // Navigation
    public Invoice Invoice { get; set; }
}

public enum PaymentMethod
{
    Cash = 1,        // Tiền mặt
    BankTransfer = 2, // Chuyển khoản
    Other = 3
}
```

### 2. RepairRequest Entity

```csharp
public class RepairRequest
{
    public long Id { get; set; }
    public long RoomId { get; set; }
    public long TenantId { get; set; }
    public RepairCategory Category { get; set; }
    public string Description { get; set; }
    public List<string> ImageUrls { get; set; }
    public RepairStatus Status { get; set; }
    public string Resolution { get; set; }      // Mô tả cách xử lý
    public decimal? Cost { get; set; }          // Chi phí sửa chữa
    public DateTime? CompletedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    
    // Navigation
    public Room Room { get; set; }
    public Tenant Tenant { get; set; }
}

public enum RepairCategory
{
    Electrical = 1,    // Điện
    Plumbing = 2,       // Nước
    Furniture = 3,      // Nội thất
    AirConditioner = 4, // Điều hòa
    Other = 5
}

public enum RepairStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
```

### 3. Me Controller (Tenant Portal)

```csharp
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IContractService _contractService;
    private readonly IInvoiceService _invoiceService;
    
    // Tenant scope all queries by current user's tenant ID
    [HttpGet("contracts")]
    public async Task<ActionResult<List<ContractDto>>> GetMyContracts();
    
    [HttpGet("invoices")]
    public async Task<ActionResult<List<InvoiceDto>>> GetMyInvoices([FromQuery] InvoiceFilterRequest filter);
}
```

### 4. Services

```csharp
public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request);
    Task<List<PaymentDto>> GetPaymentsAsync(long invoiceId);
    Task<decimal> GetRemainingAmountAsync(long invoiceId);
}

public interface IRepairRequestService
{
    // Tenant
    Task<RepairRequestDto> CreateRequestAsync(CreateRepairRequestRequest request);
    Task<List<RepairRequestDto>> GetMyRequestsAsync();
    Task<RepairRequestDto> GetMyRequestAsync(long requestId);
    
    // Owner
    Task<List<RepairRequestDto>> GetAllRequestsAsync(RepairFilterRequest filter);
    Task<RepairRequestDto> UpdateStatusAsync(long requestId, UpdateRepairStatusRequest request);
}
```

---

## 📡 API Endpoints

### Payments Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/payments | Yes | payment.read | Danh sách thanh toán |
| GET | /api/payments/{id} | Yes | payment.read | Chi tiết thanh toán |
| GET | /api/payments/invoice/{invoiceId} | Yes | payment.read | Thanh toán theo hóa đơn |
| POST | /api/payments | Yes | payment.create | Ghi nhận thanh toán |

**Create Payment Request:**
```json
{
    "invoiceId": 1,
    "amount": 5276750,
    "method": "Cash",
    "transactionId": null,
    "notes": "Thanh toán tiền phòng tháng 09/2026",
    "paidAt": "2026-09-10T14:30:00Z"
}
```

**Payment Response:**
```json
{
    "id": 1,
    "invoiceId": 1,
    "amount": 5276750,
    "method": "Cash",
    "paidAt": "2026-09-10T14:30:00Z",
    "createdAt": "2026-09-10T14:30:00Z"
}
```

### RepairRequests Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/repair-requests | Yes | repair.read | Danh sách yêu cầu (Owner) |
| GET | /api/repair-requests/{id} | Yes | repair.read | Chi tiết yêu cầu |
| POST | /api/repair-requests | Yes | repair.create | Tạo yêu cầu mới |
| PUT | /api/repair-requests/{id}/status | Yes | repair.update | Cập nhật trạng thái |
| DELETE | /api/repair-requests/{id} | Yes | repair.update | Hủy yêu cầu |

**Create Repair Request (Tenant):**
```json
{
    "roomId": 1,
    "category": "Electrical",
    "description": "Bóng đèn phòng tắm bị hỏng",
    "imageUrls": ["https://..."]
}
```

**Update Repair Status (Owner):**
```json
{
    "status": "InProgress",
    "resolution": "Đã gọi thợ đến sửa",
    "cost": 150000
}
```

### Me Controller (Tenant Portal)

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/me/contracts | Yes | contract.read.own | Hợp đồng của tôi |
| GET | /api/me/invoices | Yes | invoice.read.own | Hóa đơn của tôi |
| GET | /api/me/invoices/{id} | Yes | invoice.read.own | Chi tiết hóa đơn |
| GET | /api/me/repair-requests | Yes | repair.read.own | Yêu cầu của tôi |
| GET | /api/me/repair-requests/{id} | Yes | repair.read.own | Chi tiết yêu cầu |
| POST | /api/me/repair-requests | Yes | repair.create.own | Tạo yêu cầu mới |

---

## ✅ Definition of Done

- [ ] Ghi nhận thanh toán hoạt động đúng spec
- [ ] Validation: không thanh toán hóa đơn đã hủy
- [ ] Validation: không thanh toán vượt số tiền
- [ ] Cập nhật trạng thái hóa đơn tự động
- [ ] Tenant chỉ xem được dữ liệu của mình (BR-07)
- [ ] CRUD yêu cầu sửa chữa hoạt động
- [ ] Authorization: tenant chỉ gửi yêu cầu cho phòng của mình
- [ ] Unit tests cho PaymentService
- [ ] Unit tests cho RepairRequestService
- [ ] Authorization tests cho Me controller
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

- Sprint 2 completed (Contracts & Billing)

---

## 📝 Notes

- Payment transaction ID: optional, dùng cho tích hợp ngân hàng sau này
- Repair request images: lưu URL, không lưu file trong DB (dùng local storage hoặc cloud)
- Tenant scope: lấy tenantId từ JWT claims, không tin client
- Chi phí sửa chữa: optional, dùng cho báo cáo sau này
