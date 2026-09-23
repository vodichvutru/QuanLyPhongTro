import { useEffect, useState } from 'react'
import { api, apiDownload } from '../lib/api'
import { useToast, PageHead, Kpis, StatusBadge, EmptyRow, type KpiCard } from '../lib/ui'
import { vnd, fmtDate, monthInputVal } from '../lib/helpers'
import type { DashboardDto, DebtByRoomDto, DebtItemDto, RevenuePoint } from '../lib/types'

function monthNow() { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}` }
function monthShift(n: number) { const d = new Date(); d.setMonth(d.getMonth() + n); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}` }
function fmtK(x: number) { const n = Number(x) || 0; return n >= 1e6 ? (n / 1e6).toFixed(1) + 'tr' : (n >= 1000 ? (n / 1e3).toFixed(0) + 'k' : String(n)) }

export default function Reports() {
  const toast = useToast()
  const [dash, setDash] = useState<DashboardDto | null>(null)
  const [from, setFrom] = useState(monthShift(-4))
  const [to, setTo] = useState(monthNow())
  const [revenue, setRevenue] = useState<RevenuePoint[]>([])
  const [debt, setDebt] = useState<DebtItemDto[]>([])
  const [debtRoom, setDebtRoom] = useState<DebtByRoomDto[]>([])

  const loadRevenue = async (f = from, t = to) => {
    try {
      const rows = await api.get<RevenuePoint[]>(`/reports/revenue?from=${f}-01&to=${t}-28`)
      setRevenue(rows)
    } catch (e) { toast((e as Error).message, 'err') }
  }

  useEffect(() => {
    (async () => {
      try {
        const [d, de, dr] = await Promise.all([
          api.get<DashboardDto>('/reports/dashboard'),
          api.get<DebtItemDto[]>('/reports/debt'),
          api.get<DebtByRoomDto[]>('/reports/debt-by-room'),
        ])
        setDash(d); setDebt(de); setDebtRoom(dr)
      } catch (e) { toast((e as Error).message, 'err') }
    })()
    loadRevenue()
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const exportCsv = async () => {
    try { await apiDownload(`/reports/revenue/export?from=${from}-01&to=${to}-28`, 'doanh-thu.csv'); toast('Đã xuất CSV') }
    catch (e) { toast((e as Error).message, 'err') }
  }

  const k: KpiCard[] = dash ? [
    { t: 'Tổng phòng', v: dash.totalRooms, s: `${dash.rentedRooms} cho thuê / ${dash.availableRooms} trống` },
    { t: 'Người thuê', v: dash.totalTenants },
    { t: 'Doanh thu tháng này', v: vnd(dash.revenueThisMonth) },
    { t: 'Công nợ', v: vnd(dash.totalOutstanding), s: `${dash.openInvoices} hóa đơn` },
  ] : []

  const max = revenue.length ? Math.max(...revenue.map(r => r.revenue)) : 0

  return (
    <>
      <PageHead title="Báo cáo" actions={<button className="btn" onClick={exportCsv}>⬇ Xuất doanh thu CSV</button>} />
      {dash && <Kpis cards={k} />}

      <div className="card">
        <h3 style={{ marginTop: 0 }}>💰 Doanh thu theo tháng</h3>
        <div className="filters">
          <label style={{ margin: 0 }}>Từ</label>
          <input type="month" style={{ width: 'auto' }} value={from} onChange={e => setFrom(monthInputVal(e.target.value))} />
          <label style={{ margin: 0 }}>Đến</label>
          <input type="month" style={{ width: 'auto' }} value={to} onChange={e => setTo(monthInputVal(e.target.value))} />
          <button className="btn" onClick={() => loadRevenue()}>Xem</button>
        </div>
        {revenue.length ? (
          <div className="chart-wrap">
            <div className="chart-head">
              <span className="small muted">Cao nhất: <b>{vnd(max)}</b></span>
            </div>
            <div className="chart">
              {revenue.map((r, i) => {
                const pct = max > 0 ? (r.revenue / max) * 100 : 0
                return (
                  <div className="chart-col" key={i} title={`${r.month}: ${vnd(r.revenue)}`}>
                    <div className="chart-val">{r.revenue > 0 ? fmtK(r.revenue) : '0'}</div>
                    <div className="chart-track">
                      <div className="chart-fill" style={{ height: `${r.revenue > 0 ? Math.max(2, pct) : 0}%` }} />
                    </div>
                    <div className="chart-lbl">{r.month.slice(5)}/{r.month.slice(2, 4)}</div>
                  </div>
                )
              })}
            </div>
          </div>
        ) : <div className="empty">Chưa có dữ liệu doanh thu</div>}
        <table>
          <thead><tr><th>Tháng</th><th className="num">Doanh thu</th></tr></thead>
          <tbody>
            {revenue.length ? revenue.map((r, i) => (
              <tr key={i}><td>{r.month}</td><td className="num">{vnd(r.revenue)}</td></tr>
            )) : <EmptyRow cols={2} />}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3 style={{ marginTop: 0 }}>📋 Công nợ chi tiết</h3>
        <table>
          <thead><tr>
            <th>Hóa đơn</th><th>Phòng</th><th>Người thuê</th><th>Kỳ</th><th>Hạn</th>
            <th className="num">Tổng</th><th className="num">Còn lại</th><th>Trạng thái</th>
          </tr></thead>
          <tbody>
            {debt.length ? debt.map(x => (
              <tr key={x.invoiceId}>
                <td>{x.invoiceCode}</td><td>{x.roomName}</td><td>{x.tenantName}</td><td>{x.billingMonth}</td>
                <td>{fmtDate(x.dueDate)}</td><td className="num">{vnd(x.totalAmount)}</td>
                <td className="num"><b>{vnd(x.remaining)}</b></td><td><StatusBadge statusName={x.statusName} /></td>
              </tr>
            )) : <EmptyRow cols={8} />}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3 style={{ marginTop: 0 }}>📋 Công nợ theo phòng</h3>
        <table>
          <thead><tr><th>Phòng</th><th className="num">Hóa đơn đang nợ</th><th className="num">Tổng nợ</th></tr></thead>
          <tbody>
            {debtRoom.length ? debtRoom.map((x, i) => (
              <tr key={i}><td><b>{x.roomName}</b></td><td className="num">{x.openInvoices}</td><td className="num"><b>{vnd(x.debtAmount)}</b></td></tr>
            )) : <EmptyRow cols={3} />}
          </tbody>
        </table>
      </div>
    </>
  )
}
