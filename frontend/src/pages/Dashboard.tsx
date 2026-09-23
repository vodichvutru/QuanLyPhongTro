import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useAuth } from '../lib/auth'
import { useToast, Kpis, PageHead, StatusBadge, EmptyRow, Loading, type KpiCard } from '../lib/ui'
import { vnd, fmtDate } from '../lib/helpers'
import type { DashboardDto, DebtItemDto, InvoiceDto, ContractDto } from '../lib/types'

export default function Dashboard() {
  const { isTenant } = useAuth()
  return isTenant ? <TenantHome /> : <OwnerHome />
}

function OwnerHome() {
  const toast = useToast()
  const [dash, setDash] = useState<DashboardDto | null>(null)
  const [debt, setDebt] = useState<DebtItemDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    (async () => {
      try {
        const [d, b] = await Promise.all([
          api.get<DashboardDto>('/reports/dashboard'),
          api.get<DebtItemDto[]>('/reports/debt'),
        ])
        setDash(d); setDebt(b)
      } catch (e) { toast((e as Error).message, 'err') }
      finally { setLoading(false) }
    })()
  }, [toast])

  if (loading || !dash) return <Loading />
  const k: KpiCard[] = [
    { t: 'Tổng phòng', v: dash.totalRooms, s: `${dash.rentedRooms} đang cho thuê` },
    { t: 'Đang cho thuê', v: dash.rentedRooms, s: `Trống: ${dash.availableRooms}` },
    { t: 'Người thuê', v: dash.totalTenants, s: `Đang hoạt động: ${dash.activeTenants}` },
    { t: 'Doanh thu tháng này', v: vnd(dash.revenueThisMonth) },
    { t: 'Công nợ', v: vnd(dash.totalOutstanding), s: `${dash.openInvoices} hóa đơn chưa trả đủ` },
  ]
  return (
    <>
      <PageHead title="Tổng quan" />
      <Kpis cards={k} />
      <div className="card">
        <h3 style={{ marginTop: 0 }}>📋 Công nợ chưa thu</h3>
        <table>
          <thead><tr>
            <th>Hóa đơn</th><th>Phòng</th><th>Người thuê</th><th>Kỳ</th><th>Hạn</th>
            <th className="num">Tổng</th><th className="num">Còn lại</th>
          </tr></thead>
          <tbody>
            {debt.length ? debt.map(d => (
              <tr key={d.invoiceId}>
                <td>{d.invoiceCode}</td><td>{d.roomName}</td><td>{d.tenantName}</td>
                <td>{d.billingMonth}</td><td>{fmtDate(d.dueDate)}</td>
                <td className="num">{vnd(d.totalAmount)}</td><td className="num"><b>{vnd(d.remaining)}</b></td>
              </tr>
            )) : <EmptyRow cols={7} />}
          </tbody>
        </table>
      </div>
    </>
  )
}

function TenantHome() {
  const { user } = useAuth()
  const toast = useToast()
  const [inv, setInv] = useState<InvoiceDto[]>([])
  const [cons, setCons] = useState<ContractDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    (async () => {
      try {
        const [i, c] = await Promise.all([
          api.get<InvoiceDto[]>('/me/invoices'),
          api.get<ContractDto[]>('/me/contracts'),
        ])
        setInv(i); setCons(c)
      } catch (e) { toast((e as Error).message, 'err') }
      finally { setLoading(false) }
    })()
  }, [toast])

  if (loading) return <Loading />
  const open = inv.filter(i => i.status === 'Unpaid' || i.status === 'PartiallyPaid')
  const due = open.reduce((s, i) => s + (i.totalAmount - i.paidAmount), 0)
  const activeC = cons.filter(c => c.status === 'Active')
  const k: KpiCard[] = [
    { t: 'Hợp đồng đang hiệu lực', v: activeC.length },
    { t: 'Hóa đơn chưa thanh toán', v: open.length, s: `trên ${inv.length} hóa đơn` },
    { t: 'Số cần thanh toán', v: vnd(due), s: 'tổng các kỳ còn nợ' },
  ]
  return (
    <>
      <PageHead title={`Xin chào, ${user?.fullName || ''} 👋`} />
      <Kpis cards={k} />
      <div className="card">
        <h3 style={{ marginTop: 0 }}>🧾 Hóa đơn cần thanh toán</h3>
        <table>
          <thead><tr>
            <th>Mã</th><th>Phòng</th><th>Kỳ</th>
            <th className="num">Tổng</th><th className="num">Đã trả</th><th>Trạng thái</th>
          </tr></thead>
          <tbody>
            {open.length ? open.map(i => (
              <tr key={i.id}>
                <td>{i.invoiceCode}</td><td>{i.roomName}</td><td>{i.billingMonth}</td>
                <td className="num">{vnd(i.totalAmount)}</td><td className="num">{vnd(i.paidAmount)}</td>
                <td><StatusBadge statusName={i.statusName} /></td>
              </tr>
            )) : <EmptyRow cols={6} />}
          </tbody>
        </table>
        <div className="muted small" style={{ marginTop: 8 }}>Xem chi tiết tại mục <b>Hóa đơn của tôi</b>.</div>
      </div>
    </>
  )
}
