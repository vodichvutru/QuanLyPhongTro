// Kiểu dữ liệu khớp DTO của API (QuanLyPhongTro.Application/Dtos/Dtos.cs)

export type RoomStatus = 'Available' | 'Rented' | 'Maintenance'
export type ContractStatus = 'Active' | 'Terminated' | 'Expired'
export type InvoiceStatus = 'Unpaid' | 'PartiallyPaid' | 'Paid' | 'Cancelled'
export type PaymentMethod = 'Cash' | 'BankTransfer' | 'Momo' | 'VnPay' | 'Other'
export type RepairStatus = 'Pending' | 'Approved' | 'InProgress' | 'Completed' | 'Rejected' | 'Cancelled'

export interface UserDto {
  id: number
  username: string
  fullName: string
  email: string | null
  phone: string | null
  isActive: boolean
  roles: string[]
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: UserDto
}

export interface RoleDto { id: number; code: string; name: string; description: string | null }

export interface RoomDto {
  id: number; name: string; floor: string | null; area: number; price: number
  maxPeople: number; note: string | null; status: RoomStatus; statusName: string
  tenantName: string | null; createdAt: string
}

export interface TenantDto {
  id: number; fullName: string; phone: string | null; identityNumber: string | null
  email: string | null; address: string | null; note: string | null
  isActive: boolean; hasAccount: boolean; createdAt: string
}

export interface ContractDto {
  id: number; contractCode: string; roomId: number; roomName: string
  tenantId: number; tenantName: string; startDate: string; endDate: string | null
  monthlyRent: number; deposit: number; electricPrice: number; waterPrice: number
  status: ContractStatus; statusName: string; note: string | null; createdAt: string
}

/** Thông tin lập hóa đơn của phòng: tiền phòng, giá điện/nước, chỉ số chốt kỳ trước. */
export interface RoomBillingInfoDto {
  roomId: number; roomName: string; tenantName: string | null
  monthlyRent: number; electricPrice: number; waterPrice: number
  lastElectricIndex: number; lastWaterIndex: number
}

export interface InvoiceItemDto {
  id: number; name: string; quantity: number; unit: string | null; unitPrice: number; amount: number
}

export type PaymentStatus = 'Pending' | 'Confirmed' | 'Rejected'

export interface PaymentDto {
  id: number; invoiceId: number; amount: number; method: PaymentMethod; methodName: string
  status: PaymentStatus; statusName: string
  paidAt: string; confirmedAt: string | null
  reference: string | null; note: string | null; createdAt: string
}

export interface NotificationDto {
  id: number; type: string; title: string; message: string
  linkPath: string | null; targetRole: string; isRead: boolean; createdAt: string
}

export interface InvoiceDto {
  id: number; invoiceCode: string; contractId: number; roomId: number; roomName: string
  tenantId: number; tenantName: string; billingMonth: string
  electricOldIndex: number; electricNewIndex: number; waterOldIndex: number; waterNewIndex: number
  issueDate: string; dueDate: string | null
  totalAmount: number; paidAmount: number; pendingAmount: number; previousDebt: number
  status: InvoiceStatus; statusName: string; note: string | null
  items: InvoiceItemDto[]; payments: PaymentDto[]
}

export interface InvoicePreviewDto {
  roomName: string; tenantName: string | null; billingMonth: string
  totalAmount: number; previousDebt: number; items: InvoiceItemDto[]
}

export interface RepairRequestDto {
  id: number; roomId: number; roomName: string; tenantId: number | null; tenantName: string | null
  subject: string; description: string | null; status: RepairStatus; statusName: string
  ownerNote: string | null; cost: number | null; createdByUserId: number | null
  createdByName: string | null; createdAt: string; handledAt: string | null; handledByName: string | null
}

export interface DashboardDto {
  totalRooms: number; rentedRooms: number; availableRooms: number; maintenanceRooms: number
  totalTenants: number; activeTenants: number; openInvoices: number
  totalOutstanding: number; revenueThisMonth: number
}

export interface RevenuePoint { month: string; revenue: number }

export interface DebtItemDto {
  invoiceId: number; invoiceCode: string; roomName: string; tenantName: string
  billingMonth: string; dueDate: string | null; totalAmount: number; paidAmount: number
  remaining: number; statusName: string
}

export interface DebtByRoomDto { roomName: string; openInvoices: number; debtAmount: number }

export interface ExtraFeeLine { name: string; amount: number }

// ---- request payloads ----
export interface LoginRequest { username: string; password: string }
export interface CreateRoomRequest { name: string; floor?: string | null; area: number; price: number; maxPeople: number; note?: string | null }
export interface UpdateRoomRequest extends CreateRoomRequest { status?: RoomStatus | null }
export interface CreateTenantRequest {
  fullName: string; phone?: string | null; identityNumber?: string | null; email?: string | null
  address?: string | null; note?: string | null; username?: string | null; password?: string | null
}
export interface UpdateTenantRequest {
  fullName: string; phone?: string | null; identityNumber?: string | null; email?: string | null
  address?: string | null; note?: string | null; isActive: boolean
}
export interface CreateContractRequest {
  roomId: number; tenantId: number; startDate: string; endDate?: string | null
  monthlyRent?: number; deposit?: number; electricPrice?: number; waterPrice?: number; note?: string | null
}
export interface CreateMeterReadingRequest { roomId: number; readingDate: string; electricIndex: number; waterIndex: number; note?: string | null }
export interface CreateInvoiceRequest {
  roomId: number; billingMonth: string
  electricOldIndex: number; electricNewIndex: number; waterOldIndex: number; waterNewIndex: number
  dueDate?: string | null; note?: string | null; extraItems?: ExtraFeeLine[] | null
}
export interface CreatePaymentRequest { invoiceId: number; amount: number; method: PaymentMethod; paidAt?: string | null; reference?: string | null; note?: string | null }
export interface CreateMyPaymentRequest { method: PaymentMethod; reference?: string | null; note?: string | null }
export interface CreateMyRepairRequestRequest { roomId: number; subject: string; description?: string | null }
export interface CreateRepairRequestRequest { roomId: number; subject: string; description?: string | null; tenantId?: number | null }
export interface UpdateRepairStatusRequest { status: RepairStatus; ownerNote?: string | null; cost?: number | null }
export interface CreateUserRequest { username: string; password: string; fullName: string; email?: string | null; phone?: string | null; roles?: string[] | null }
export interface UpdateUserRolesRequest { roles: string[] }
export interface SetUserActiveRequest { isActive: boolean }
export interface ResetPasswordRequest { newPassword: string }
