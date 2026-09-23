import type { ContractStatus, InvoiceStatus, PaymentMethod, RepairStatus, RoomStatus } from './types'

/* ---------- định dạng ---------- */
export function vnd(x: number | null | undefined): string {
  const n = Number(x) || 0
  return n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫'
}
export function dnum(x: number | null | undefined): string {
  const n = Number(x) || 0
  return n.toLocaleString('vi-VN', { maximumFractionDigits: 2 })
}
export function fmtDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  return isNaN(d.getTime()) ? String(iso) : d.toLocaleDateString('vi-VN')
}
/** 'YYYY-MM-DD...' -> 'YYYY-MM' cho input type=month */
export function monthInputVal(v: string | null | undefined): string {
  if (!v) return ''
  const m = String(v).match(/^(\d{4})-(\d{2})/)
  return m ? `${m[1]}-${m[2]}` : v
}
/** Date -> 'YYYY-MM-DD' cho input type=date */
export function dateInputVal(iso: string | null | undefined): string {
  if (!iso) return ''
  const d = new Date(iso)
  if (isNaN(d.getTime())) return ''
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
}

/* ---------- nhãn enum ---------- */
export function roleName(c: string): string {
  return ({ Admin: 'Quản trị viên', Owner: 'Chủ trọ', Tenant: 'Người thuê' } as Record<string, string>)[c] || c
}
export function roomStatusName(s: RoomStatus): string {
  return ({ Available: 'Trống', Rented: 'Đang cho thuê', Maintenance: 'Đang bảo trì' } as Record<string, string>)[s] || s
}
export function contractStatusName(s: ContractStatus): string {
  return ({ Active: 'Đang hiệu lực', Terminated: 'Đã chấm dứt', Expired: 'Hết hạn' } as Record<string, string>)[s] || s
}
export function invoiceStatusName(s: InvoiceStatus): string {
  return ({ Unpaid: 'Chưa thanh toán', PartiallyPaid: 'Thanh toán một phần', Paid: 'Đã thanh toán', Cancelled: 'Đã hủy' } as Record<string, string>)[s] || s
}
export function repairStatusName(s: RepairStatus): string {
  return ({ Pending: 'Chờ xử lý', Approved: 'Đã tiếp nhận', InProgress: 'Đang xử lý', Completed: 'Đã hoàn thành', Rejected: 'Từ chối', Cancelled: 'Đã hủy' } as Record<string, string>)[s] || s
}
export function paymentMethodName(m: PaymentMethod): string {
  return ({ Cash: 'Tiền mặt', BankTransfer: 'Chuyển khoản', Momo: 'Momo', VnPay: 'VNPay', Other: 'Khác' } as Record<string, string>)[m] || m
}

/* ---------- màu badge theo tên trạng thái ---------- */
export function statusKind(statusName: string): '' | 'ok' | 'warn' | 'bad' {
  const s = String(statusName || '')
  if (/chưa thanh toán/.test(s)) return 'bad'
  if (/hủy|chấm dứt|hết hạn|từ chối|bảo trì/.test(s)) return 'bad'
  if (/chờ|một phần/.test(s)) return 'warn'
  if (/thanh toán|đã|hoàn|cho thuê|hiệu lực|tiếp nhận|đang xử|trống/.test(s)) return 'ok'
  return ''
}

export const PAYMENT_METHODS: PaymentMethod[] = ['Cash', 'BankTransfer', 'Momo', 'VnPay', 'Other']
export const REPAIR_STATUSES: RepairStatus[] = ['Pending', 'Approved', 'InProgress', 'Completed', 'Rejected', 'Cancelled']
