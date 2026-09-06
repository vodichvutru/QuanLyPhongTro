# Sprint 4: Polish & Testing

**Thời gian:** Tuần 8  
**Mục tiêu:** Báo cáo, testing, fix bugs, documentation

---

## 🎯 Sprint Goal

Hoàn thành các công việc còn lại:
- Báo cáo doanh thu và công nợ
- Integration testing toàn bộ system
- Bug fixes
- Documentation và deployment preparation

---

## 📋 User Stories

### US-011: Báo cáo doanh thu và công nợ

**Actor:** Chủ trọ (Owner)  
**Mô tả:** Là chủ trọ, tôi muốn xem báo cáo doanh thu và công nợ, để theo dõi hoạt động kinh doanh.

**Acceptance Criteria:**
- [ ] AC-01: Owner có thể xem báo cáo tổng quan (tổng thu, tổng công nợ)
- [ ] AC-02: Owner có thể xem báo cáo theo tháng
- [ ] AC-03: Owner có thể xem báo cáo theo phòng
- [ ] AC-04: Owner có thể export báo cáo (Excel/PDF)

**Report Types:**
- Dashboard: Tổng quan tài chính
- Revenue Report: Doanh thu theo tháng
- Debt Report: Công nợ theo phòng
- Collection Report: Tỷ lệ thu tiền

---

## 🛠️ Technical Tasks

### 1. Report DTOs

```csharp
public class DashboardDto
{
    public int TotalRooms { get; set; }
    public int RentedRooms { get; set; }
    public int AvailableRooms { get; set; }
    
    public decimal TotalRevenue { get; set; }        // Tháng hiện tại
    public decimal TotalDebt { get; set; }           // Công nợ
    public decimal CollectionRate { get; set; }       // Tỷ lệ thu tiền
    
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; }
    public List<DebtByRoomDto> TopDebts { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; }  // "2026-09"
    public decimal Revenue { get; set; }
    public decimal Debt { get; set; }
    public int InvoiceCount { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
}

public class DebtByRoomDto
{
    public long RoomId { get; set; }
    public string RoomNumber { get; set; }
    public decimal DebtAmount { get; set; }
    public int OverdueMonths { get; set; }
}
```

### 2. Report Service

```csharp
public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(DateTime from, DateTime to);
    Task<List<DebtByRoomDto>> GetDebtReportAsync();
    Task<byte[]> ExportRevenueReportAsync(ExportFormat format, DateTime from, DateTime to);
}

public enum ExportFormat
{
    Excel = 1,
    PDF = 2
}
```

### 3. Integration Tests

```csharp
[Fact]
public async Task CreateInvoice_WithValidData_ReturnsCreated()
{
    // Arrange
    await SetupTestData();
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/invoices", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var invoice = await response.Content.ReadFromJsonAsync<InvoiceDto>();
    invoice.Status.Should().Be(InvoiceStatus.Unpaid);
}

[Fact]
public async Task CreateInvoice_DuplicatePeriod_ReturnsConflict()
{
    // Arrange
    await _invoiceRepository.CreateAsync(existingInvoice);
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/invoices", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
}

[Fact]
public async Task Me_Invoices_ReturnsOnlyOwnData()
{
    // Arrange
    var tenantToken = await GetTenantToken();
    await _invoiceRepository.CreateAsync(invoiceForOtherTenant);
    
    // Act
    var response = await _client
        .WithAuth(tenantToken)
        .GetAsync("/api/me/invoices");
    
    // Assert
    var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceDto>>();
    invoices.Should().NotContain(i => i.RoomId == otherTenantRoomId);
}

[Fact]
public async Task Payment_FullyPaid_UpdatesInvoiceStatus()
{
    // Arrange
    var invoice = await CreateUnpaidInvoice(1000000);
    
    // Act
    await _client.PostAsJsonAsync("/api/payments", new 
    {
        invoiceId = invoice.Id,
        amount = 1000000
    });
    
    // Assert
    var updatedInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id);
    updatedInvoice.Status.Should().Be(InvoiceStatus.Paid);
}
```

### 4. Test Coverage

```
Controllers/
├── AuthControllerTests.cs         [Coverage: 80%]
├── RoomControllerTests.cs          [Coverage: 75%]
├── TenantControllerTests.cs        [Coverage: 75%]
├── ContractControllerTests.cs       [Coverage: 70%]
├── MeterReadingControllerTests.cs  [Coverage: 70%]
├── InvoiceControllerTests.cs        [Coverage: 70%]
├── PaymentControllerTests.cs        [Coverage: 70%]
├── RepairRequestControllerTests.cs  [Coverage: 70%]
└── MeControllerTests.cs            [Coverage: 80%]

Services/
├── RoomServiceTests.cs             [Coverage: 85%]
├── TenantServiceTests.cs           [Coverage: 85%]
├── ContractServiceTests.cs         [Coverage: 80%]
├── BillingServiceTests.cs          [Coverage: 85%]
├── PaymentServiceTests.cs          [Coverage: 80%]
└── RepairRequestServiceTests.cs    [Coverage: 80%]
```

### 5. Bug Fixes

- [ ] Collect all bugs from previous sprints
- [ ] Prioritize bugs by severity
- [ ] Fix P0 (Critical) bugs
- [ ] Fix P1 (High) bugs
- [ ] Regression testing

### 6. Documentation

- [ ] README.md - Project overview, setup instructions
- [ ] API Documentation (Swagger)
- [ ] Database schema documentation
- [ ] Deployment guide

---

## 📡 API Endpoints

### Reports Controller

| Method | Endpoint | Auth | Permission | Description |
|--------|----------|------|------------|-------------|
| GET | /api/reports/dashboard | Yes | room.read | Dashboard tổng quan |
| GET | /api/reports/revenue | Yes | room.read | Doanh thu theo tháng |
| GET | /api/reports/debt | Yes | room.read | Công nợ theo phòng |
| GET | /api/reports/revenue/export | Yes | room.read | Export báo cáo |

**Query Parameters:**
```
GET /api/reports/revenue?from=2026-01&to=2026-09&format=Excel
GET /api/reports/revenue?from=2026-01&to=2026-09&format=PDF
```

---

## ✅ Definition of Done

### Reports
- [ ] Dashboard API trả dữ liệu đúng
- [ ] Revenue report theo tháng hoạt động
- [ ] Debt report theo phòng hoạt động
- [ ] Export Excel/PDF hoạt động

### Testing
- [ ] Integration tests cho tất cả APIs
- [ ] Authorization tests cho protected endpoints
- [ ] Business rule tests (duplicate prevention, payment validation)
- [ ] Code coverage > 60%

### Bug Fixes
- [ ] All P0 bugs fixed
- [ ] All P1 bugs fixed
- [ ] No new critical bugs introduced

### Documentation
- [ ] README.md hoàn chỉnh
- [ ] Swagger đầy đủ (descriptions, examples)
- [ ] Deployment guide có sẵn

### Deployment
- [ ] CI/CD pipeline configured
- [ ] Production build successful
- [ ] Database migration scripts ready

---

## 📊 Metrics

| Metric | Target |
|--------|--------|
| Velocity | - |
| Tasks Completed | - |
| Code Coverage | >60% |
| API Tests | >90% passed |
| Bug Count | 0 P0/P1 |

---

## 🔗 Dependencies

- Sprint 3 completed (Payments & Self-service)

---

## 📝 Notes

### Export Implementation
- Excel: Use EPPlus or ClosedXML library
- PDF: Use QuestPDF or iTextSharp library
- File storage: Return as download, don't store on server

### Performance Considerations
- Dashboard queries: Consider caching (15 min TTL)
- Report queries: Add database indexes if needed
- Large exports: Consider async job + download link

### Deployment Checklist
- [ ] Remove seed data in production
- [ ] Set strong JWT secret
- [ ] Configure production database
- [ ] Setup SSL certificate
- [ ] Backup strategy documented
- [ ] Monitoring configured
