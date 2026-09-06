# Sprint Plans - Tổng quan

Dự án: **HỆ THỐNG QUẢN LÝ PHÒNG TRỌ VÀ THANH TOÁN TIỀN THUÊ**

---

## 📅 Lịch sử Sprint

| Sprint | Thời gian | Tên | Status |
|--------|------------|------|--------|
| Sprint 0 | Tuần 1 | Setup & Foundation | ✅ Đã làm (cơ bản) |
| Sprint 1 | Tuần 2-3 | Room Management | ✅ Đã làm (core) |
| Sprint 2 | Tuần 4-5 | Contracts & Billing | ✅ Đã làm (core) |
| Sprint 1 | Tuần 2-3 | Room Management | 📋 Pending |
| Sprint 2 | Tuần 4-5 | Contracts & Billing | 📋 Pending |
| Sprint 3 | Tuần 6-7 | Payments & Self-service | ✅ Đã làm (core) |
| Sprint 4 | Tuần 8 | Polish & Testing | ✅ Đã làm (reports + tests + docs) |

---

## 📋 User Stories Summary

| ID | Sprint | User Story | Priority | Status |
|----|--------|-----------|----------|--------|
| US-001 | Sprint 0 | Login & JWT Authentication | Must | ✅ |
| US-010 | Sprint 0 | RBAC - Quản lý User/Role | Must | ✅ |
| US-002 | Sprint 1 | Quản lý phòng trọ | Must | ✅ |
| US-003 | Sprint 1 | Quản lý người thuê | Must | ✅ |
| US-004 | Sprint 2 | Quản lý hợp đồng thuê | Must | ✅ |
| US-005 | Sprint 2 | Ghi nhận chỉ số điện nước | Must | ✅ |
| US-006 | Sprint 2 | Tạo hóa đơn tiền thuê | Must | ✅ |
| US-007 | Sprint 3 | Ghi nhận thanh toán | Must | ✅ |
| US-008 | Sprint 3 | Tenant tra cứu hợp đồng/hóa đơn | Must | ✅ |
| US-009 | Sprint 3 | Tenant gửi yêu cầu sửa chữa | Should | ✅ |
| US-010 | Sprint 3 | Chủ trọ xử lý yêu cầu sửa chữa | Should | ✅ |
| US-011 | Sprint 4 | Báo cáo doanh thu và công nợ | Should | ✅ |

---

## 📊 API Endpoints Summary

### Public
| Method | Endpoint | Sprint |
|--------|----------|--------|
| POST | /api/auth/login | Sprint 0 |
| POST | /api/auth/refresh | Sprint 0 |

### Admin (Owner)
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET | /api/auth/me | Sprint 0 |
| GET/POST/PUT/DELETE | /api/admin/users | Sprint 0 |
| GET/POST/PUT | /api/admin/roles | Sprint 0 |
| GET | /api/admin/permissions | Sprint 0 |

### Rooms
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET/POST | /api/rooms | Sprint 1 |
| GET/PUT/PATCH/DELETE | /api/rooms/{id} | Sprint 1 |

### Tenants
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET/POST | /api/tenants | Sprint 1 |
| GET/PUT/DELETE | /api/tenants/{id} | Sprint 1 |

### Contracts
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET/POST | /api/contracts | Sprint 2 |
| GET/PUT | /api/contracts/{id} | Sprint 2 |
| GET | /api/contracts/room/{roomId} | Sprint 2 |
| POST | /api/contracts/{id}/terminate | Sprint 2 |

### Meter Readings
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET | /api/meter-readings | Sprint 2 |
| GET/POST | /api/meter-readings/room/{roomId} | Sprint 2 |
| POST | /api/meter-readings | Sprint 2 |
| PUT | /api/meter-readings/{id} | Sprint 2 |

### Invoices
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET/POST | /api/invoices | Sprint 2 |
| GET | /api/invoices/{id} | Sprint 2 |
| GET | /api/invoices/room/{roomId} | Sprint 2 |
| DELETE | /api/invoices/{id} | Sprint 2 |

### Payments
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET/POST | /api/payments | Sprint 3 |
| GET | /api/payments/{id} | Sprint 3 |
| GET | /api/payments/invoice/{invoiceId} | Sprint 3 |

### Repair Requests
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET/POST | /api/repair-requests | Sprint 3 |
| GET | /api/repair-requests/{id} | Sprint 3 |
| PUT | /api/repair-requests/{id}/status | Sprint 3 |
| DELETE | /api/repair-requests/{id} | Sprint 3 |

### Tenant Portal (Me)
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET | /api/me/contracts | Sprint 3 |
| GET | /api/me/invoices | Sprint 3 |
| GET | /api/me/invoices/{id} | Sprint 3 |
| GET/POST | /api/me/repair-requests | Sprint 3 |
| GET | /api/me/repair-requests/{id} | Sprint 3 |

### Reports
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET | /api/reports/dashboard | Sprint 4 |
| GET | /api/reports/revenue | Sprint 4 |
| GET | /api/reports/debt | Sprint 4 |
| GET | /api/reports/revenue/export | Sprint 4 |

### Settings
| Method | Endpoint | Sprint |
|--------|----------|--------|
| GET | /api/settings | Sprint 2 |
| PUT | /api/settings | Sprint 2 |

---

## 🔐 Permission Matrix

| Permission | Admin | Owner | Tenant |
|------------|-------|-------|--------|
| auth.login | ✅ | ✅ | ✅ |
| user.* | ✅ | ❌ | ❌ |
| role.* | ✅ | ❌ | ❌ |
| room.* | ✅ | ✅ | ❌ |
| tenant.* | ✅ | ✅ | ❌ |
| contract.* | ✅ | ✅ | ❌ |
| meter.* | ✅ | ✅ | ❌ |
| invoice.* | ✅ | ✅ | ❌ |
| payment.* | ✅ | ✅ | ❌ |
| repair.* | ✅ | ✅ | ❌ |
| invoice.read.own | ❌ | ❌ | ✅ |
| repair.create.own | ❌ | ❌ | ✅ |
| contract.read.own | ❌ | ❌ | ✅ |

---

## 📁 File Structure

```
Sprints/
├── Sprint-Overview.md              # File này
├── 00-Sprint-0-Setup-Foundation.md
├── 01-Sprint-1-Room-Management.md
├── 02-Sprint-2-Contracts-Billing.md
├── 03-Sprint-3-Payments-SelfService.md
└── 04-Sprint-4-Polish-Testing.md
```

---

## 🚀 Getting Started

### Sprint 0 Checklist
- [ ] Tạo project structure
- [ ] Setup database với EF Core
- [ ] Implement JWT authentication
- [ ] Implement RBAC
- [ ] Write unit tests

### Sprint 1 Checklist
- [ ] Implement Room CRUD
- [ ] Implement Tenant CRUD
- [ ] Add authorization
- [ ] Write unit tests

### Sprint 2 Checklist
- [ ] Implement Contract management
- [ ] Implement Meter Readings
- [ ] Implement Invoice creation
- [ ] Add billing service
- [ ] Write integration tests

### Sprint 3 Checklist
- [ ] Implement Payment recording
- [ ] Implement Tenant portal (Me controller)
- [ ] Implement Repair Requests
- [ ] Write authorization tests

### Sprint 4 Checklist
- [ ] Implement Reports
- [ ] Write integration tests
- [ ] Fix bugs
- [ ] Update documentation
- [ ] Prepare deployment

---

## 📞 Communication

- **Daily Standup:** Thứ 2-6, 8:00 AM (Discord/Zoom)
- **Sprint Planning:** Thứ 6 cuối sprint
- **Sprint Review:** Thứ 6 đầu sprint
- **Sprint Retrospective:** Thứ 6 đầu sprint

---

## 👥 Team Roles

| Role | Member | Responsibilities |
|------|--------|-----------------|
| Product Owner | Nguyễn Tiến Dũng | Backlog, priorities |
| Tech Lead | Trần Phong | Architecture, code review |
| Backend Dev 1 | Trần Phong | Auth, Billing |
| Backend Dev 2 | Nguyễn Ngọc Tuấn | Rooms, Contracts |
| QA | Nhóm | Testing, bugs |

---

## 📈 Definition of Done

Mỗi task cần đạt:
- ✅ Code hoàn thành
- ✅ Unit tests passed
- ✅ Code review approved
- ✅ Merged to main branch
- ✅ Swagger documentation updated

---

## 🎯 Definition of Ready

Mỗi User Story cần:
- ☐ Mô tả rõ ràng (actor, action, value)
- ☐ Acceptance Criteria cụ thể
- ☐ Đã estimate effort
- ☐ Dependencies đã xác định
- ☐ Team đã hiểu và đồng ý
