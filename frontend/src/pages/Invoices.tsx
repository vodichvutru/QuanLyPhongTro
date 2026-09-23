import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, StatusBadge, Modal, DetailRow, EmptyRow, Loading } from '../lib/ui'
import { vnd, dnum, fmtDate, monthInputVal, invoiceStatusName, paymentMethodName, PAYMENT_METHODS } from '../lib/helpers'
import type { CreateInvoiceRequest, ExtraFeeLine, InvoiceDto, InvoicePreviewDto, PaymentMethod, RoomBillingInfoDto, RoomDto } from '../lib/types'

const STATUSES = ['Unpaid', 'PartiallyPaid', 'Paid', 'Cancelled'] as const

function monthNow() { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}` }

export default function Invoices() {
  const toast = useToast()
  const [month, setMonth] = useState(monthNow())
  const [status, setStatus] = useState('')
  const [list, setList] = useState<InvoiceDto[]>([])
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)
  const [viewId, setViewId] = useState<number | null>(null)

  const load = async () => {
    setLoading(true)
    try {
      const qs = new URLSearchParams()
      if (month) qs.set('month', month)
      if (status) qs.set('status', status)
      setList(await api.get<InvoiceDto[]>('/invoices?' + qs.toString()))
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [month, status]) // eslint-disable-line react-hooks/exhaustive-deps

  const cancel = async (id: number) => {
    if (!confirm('Hủy hóa đơn này?')) return
    try { await api.post(`/invoices/${id}/cancel`); toast('Đã hủy'); load() }
    catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <>
      <PageHead title="Hóa đơn tiền thuê" actions={<button className="btn" onClick={() => setCreating(true)}>+ Lập hóa đơn</button>} />
      <div className="filters">
        <label style={{ margin: 0 }}>Kỳ</label>
        <input type="month" style={{ width: 'auto' }} value={month} onChange={e => setMonth(monthInputVal(e.target.value))} />
        <select style={{ width: 'auto' }} value={status} onChange={e => setStatus(e.target.value)}>
          <option value="">Tất cả trạng thái</option>
          {STATUSES.map(s => <option key={s} value={s}>{invoiceStatusName(s)}</option>)}
        </select>
      </div>
      <div className="card">
        <table>
          <thead><tr>
            <th>Mã</th><th>Kỳ</th><th>Phòng</th><th>Người thuê</th>
            <th className="num">Tổng</th><th className="num">Đã trả</th><th>Trạng thái</th><th style={{ width: 170 }}></th>
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={8}><Loading /></td></tr>
              : list.length ? list.map(i => (
                <tr key={i.id}>
                  <td>{i.invoiceCode}</td><td>{i.billingMonth}</td><td><b>{i.roomName}</b></td><td>{i.tenantName}</td>
                  <td className="num">{vnd(i.totalAmount)}</td>
                  <td className="num">
                    {vnd(i.paidAmount)}
                    {i.pendingAmount > 0 && <div className="small" style={{ color: 'var(--warn)' }}>chờ xác nhận {vnd(i.pendingAmount)}</div>}
                  </td>
                  <td><StatusBadge statusName={i.statusName} /></td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    <button className="btn gray sm" onClick={() => setViewId(i.id)}>Xem &amp; thu</button>{' '}
                    {i.status !== 'Cancelled' && <button className="btn danger sm" onClick={() => cancel(i.id)}>Hủy</button>}
                  </td>
                </tr>
              )) : <EmptyRow cols={8} />}
          </tbody>
        </table>
      </div>

      {creating && <InvoiceForm onClose={() => setCreating(false)} onSaved={() => { setCreating(false); load() }} />}
      {viewId != null && <InvoiceView id={viewId} onClose={() => setViewId(null)} onChanged={load} />}
    </>
  )
}

/** Lập hóa đơn: chọn phòng + kỳ, NHẬP CHỈ SỐ điện/nước → máy tính tiền điện/nước + cộng tiền phòng. */
function InvoiceForm({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [rooms, setRooms] = useState<RoomDto[]>([])
  const [roomId, setRoomId] = useState('')
  const [info, setInfo] = useState<RoomBillingInfoDto | null>(null)
  const [month, setMonth] = useState(monthNow())
  const [due, setDue] = useState('')
  const [eOld, setEOld] = useState('0')
  const [eNew, setENew] = useState('')
  const [wOld, setWOld] = useState('0')
  const [wNew, setWNew] = useState('')
  const [extras, setExtras] = useState<ExtraFeeLine[]>([])
  const [preview, setPreview] = useState<InvoicePreviewDto | null>(null)
  const [previewErr, setPreviewErr] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    (async () => {
      try {
        const r = await api.get<RoomDto[]>('/rooms')
        setRooms(r)
        if (r.length) setRoomId(String(r[0].id))
      } catch (e) { toast((e as Error).message, 'err') }
    })()
  }, [toast])

  // Đổi phòng → nạp tiền phòng, giá điện/nước và chỉ số chốt kỳ trước (làm chỉ số đầu kỳ).
  useEffect(() => {
    if (!roomId) return
    (async () => {
      try {
        const i = await api.get<RoomBillingInfoDto>(`/invoices/billing-info/${roomId}`)
        setInfo(i)
        setEOld(String(i.lastElectricIndex))
        setWOld(String(i.lastWaterIndex))
      } catch (e) { toast((e as Error).message, 'err') }
    })()
  }, [roomId, toast])

  const payload = (): CreateInvoiceRequest => ({
    roomId: Number(roomId), billingMonth: month,
    electricOldIndex: Number(eOld) || 0, electricNewIndex: Number(eNew) || 0,
    waterOldIndex: Number(wOld) || 0, waterNewIndex: Number(wNew) || 0,
    extraItems: extras.filter(x => x.name && Number(x.amount) > 0).length
      ? extras.filter(x => x.name && Number(x.amount) > 0) : null,
  })

  // Dự toán (debounce) — xem trước số tiền trước khi lưu.
  useEffect(() => {
    if (!roomId || !month || eNew === '' || wNew === '') { setPreview(null); return }
    const t = setTimeout(async () => {
      try {
        setPreview(await api.post<InvoicePreviewDto>('/invoices/preview', payload()))
        setPreviewErr('')
      } catch (e) { setPreview(null); setPreviewErr((e as Error).message) }
    }, 250)
    return () => clearTimeout(t)
  }, [roomId, month, eOld, eNew, wOld, wNew, extras]) // eslint-disable-line react-hooks/exhaustive-deps

  const kwh = Math.max(0, (Number(eNew) || 0) - (Number(eOld) || 0))
  const m3 = Math.max(0, (Number(wNew) || 0) - (Number(wOld) || 0))

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (eNew === '' || wNew === '') { toast('Nhập chỉ số điện và nước cuối kỳ.', 'err'); return }
    setBusy(true)
    try {
      await api.post('/invoices', { ...payload(), dueDate: due || null })
      toast('Đã lập hóa đơn'); onSaved()
    } catch (err) { toast((err as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title="Lập hóa đơn (nhập chỉ số điện/nước)" onClose={onClose}
      footer={<>
        <button className="btn gray" type="button" onClick={onClose}>Đóng</button>
        <button className="btn" type="submit" form="invoice-form" disabled={busy}>Lập hóa đơn</button>
      </>}>
      <div className="small muted" style={{ marginTop: 6 }}>
        Nhập chỉ số điện và nước cuối kỳ — hệ thống tính tiền điện/nước theo chỉ số tiêu thụ rồi cộng tiền phòng hàng tháng.
      </div>
      <form id="invoice-form" onSubmit={submit}>
        <div className="form-grid">
          <div>
            <label>Phòng</label>
            <select value={roomId} onChange={e => setRoomId(e.target.value)}>
              {rooms.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select>
          </div>
          <div><label>Kỳ thanh toán</label><input type="month" value={month} onChange={e => setMonth(monthInputVal(e.target.value))} /></div>
          <div><label>Hạn thanh toán (bỏ trống = cuối kỳ)</label><input type="date" value={due} onChange={e => setDue(e.target.value)} /></div>
        </div>

        {info && (
          <div className="muted small" style={{ margin: '8px 0' }}>
            {info.tenantName ? <>Người thuê: <b>{info.tenantName}</b> · </> : ''}
            Tiền phòng: <b>{vnd(info.monthlyRent)}</b> · Điện <b>{vnd(info.electricPrice)}/kWh</b> · Nước <b>{vnd(info.waterPrice)}/m³</b>
          </div>
        )}

        <div className="form-grid">
          <div><label>Chỉ số điện đầu kỳ (kWh)</label><input type="number" step="any" value={eOld} onChange={e => setEOld(e.target.value)} /></div>
          <div><label>Chỉ số điện cuối kỳ (kWh)</label><input type="number" step="any" value={eNew} onChange={e => setENew(e.target.value)} /></div>
          <div><label>Chỉ số nước đầu kỳ (m³)</label><input type="number" step="any" value={wOld} onChange={e => setWOld(e.target.value)} /></div>
          <div><label>Chỉ số nước cuối kỳ (m³)</label><input type="number" step="any" value={wNew} onChange={e => setWNew(e.target.value)} /></div>
        </div>
        {eNew !== '' && wNew !== '' && (
          <div className="muted small" style={{ marginTop: 4 }}>
            Tiêu thụ kỳ này: <b>{dnum(kwh)} kWh</b> điện · <b>{dnum(m3)} m³</b> nước
          </div>
        )}

        <label>Phí thêm (không bắt buộc)</label>
        <div>
          {extras.map((ex, i) => (
            <div key={i} style={{ display: 'flex', gap: 8, marginTop: 6 }}>
              <input placeholder="Tên phí (VD: Phí vệ sinh)" style={{ flex: 2 }}
                value={ex.name} onChange={e => setExtras(a => a.map((x, j) => j === i ? { ...x, name: e.target.value } : x))} />
              <input type="number" placeholder="Số tiền" style={{ flex: 1 }}
                value={ex.amount || ''} onChange={e => setExtras(a => a.map((x, j) => j === i ? { ...x, amount: Number(e.target.value) } : x))} />
              <button type="button" className="btn gray sm" onClick={() => setExtras(a => a.filter((_, j) => j !== i))}>✕</button>
            </div>
          ))}
        </div>
        <button type="button" className="btn gray sm" style={{ marginTop: 6 }} onClick={() => setExtras(a => [...a, { name: '', amount: 0 }])}>+ Thêm khoản phí</button>

        <div style={{ marginTop: 12 }}>
          {previewErr ? <div className="empty" style={{ padding: 8 }}>{previewErr}</div>
            : preview ? (
              <div className="card" style={{ padding: 12 }}>
                <h4 style={{ marginTop: 0 }}>🧾 Dự kiến hóa đơn — {preview.roomName}{preview.tenantName ? ` (${preview.tenantName})` : ''}</h4>
                <table>
                  <thead><tr><th>Khoản</th><th>SL</th><th>Đơn giá</th><th className="num">Thành tiền</th></tr></thead>
                  <tbody>
                    {preview.items.length ? preview.items.map((x, i) => (
                      <tr key={i}>
                        <td>{x.name}</td>
                        <td>{x.unit ? `${dnum(x.quantity)} ${x.unit}` : '—'}</td>
                        <td>{vnd(x.unitPrice)}</td>
                        <td className="num">{vnd(x.amount)}</td>
                      </tr>
                    )) : <tr><td colSpan={4} className="empty">Không có khoản nào</td></tr>}
                  </tbody>
                </table>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 6 }} className="small muted">
                  <span>Nợ các kỳ trước (hiển thị riêng, chưa cộng vào tổng)</span><b>{vnd(preview.previousDebt)}</b>
                </div>
                <div style={{ textAlign: 'right', fontWeight: 700, fontSize: 15, marginTop: 4 }}>
                  Dự kiến tổng hóa đơn: {vnd(preview.totalAmount)}
                </div>
              </div>
            ) : <div className="muted small">Nhập chỉ số cuối kỳ để xem dự toán.</div>}
        </div>
      </form>
    </Modal>
  )
}

function InvoiceView({ id, onClose, onChanged }: { id: number; onClose: () => void; onChanged: () => void }) {
  const toast = useToast()
  const [inv, setInv] = useState<InvoiceDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [amount, setAmount] = useState('')
  const [method, setMethod] = useState<PaymentMethod>('Cash')
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const i = await api.get<InvoiceDto>('/invoices/' + id)
      setInv(i); setAmount(String(i.totalAmount - i.paidAmount))
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [id]) // eslint-disable-line react-hooks/exhaustive-deps

  const pay = async () => {
    const amt = Number(amount)
    if (!(amt > 0)) { toast('Nhập số tiền hợp lệ.', 'err'); return }
    setBusy(true)
    try {
      await api.post('/payments', { invoiceId: id, amount: amt, method, note: note || null })
      toast('Đã ghi nhận thanh toán'); onChanged(); load()
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  const confirmPayment = async (pid: number) => {
    try { await api.post(`/payments/${pid}/confirm`); toast('Đã xác nhận thanh toán'); onChanged(); load() }
    catch (e) { toast((e as Error).message, 'err') }
  }
  const rejectPayment = async (pid: number) => {
    const reason = prompt('Lý do từ chối (bỏ trống cũng được):') ?? ''
    try { await api.post(`/payments/${pid}/reject`, { reason: reason || null }); toast('Đã từ chối khoản thanh toán'); onChanged(); load() }
    catch (e) { toast((e as Error).message, 'err') }
  }

  if (loading || !inv) return <Modal title="Hóa đơn" onClose={onClose}><Loading /></Modal>
  const rem = inv.totalAmount - inv.paidAmount - inv.pendingAmount

  return (
    <Modal title={`Hóa đơn ${inv.invoiceCode}`} onClose={onClose}
      footer={<button className="btn gray" onClick={onClose}>Đóng</button>}>
      <div style={{ margin: '10px 0' }}>
        <DetailRow label="Phòng / Người thuê" value={`${inv.roomName} — ${inv.tenantName}`} />
        <DetailRow label="Kỳ" value={inv.billingMonth} />
        <DetailRow label="Chỉ số điện (đầu → cuối)" value={`${dnum(inv.electricOldIndex)} → ${dnum(inv.electricNewIndex)} kWh`} />
        <DetailRow label="Chỉ số nước (đầu → cuối)" value={`${dnum(inv.waterOldIndex)} → ${dnum(inv.waterNewIndex)} m³`} />
        <DetailRow label="Hạn thanh toán" value={fmtDate(inv.dueDate)} />
        <DetailRow label="Nợ kỳ trước" value={vnd(inv.previousDebt)} />
        <DetailRow label="Trạng thái" value={<StatusBadge statusName={inv.statusName} />} />
      </div>

      <h4>Chi tiết</h4>
      <table>
        <thead><tr><th>Khoản</th><th>SL</th><th>Đơn giá</th><th className="num">Thành tiền</th></tr></thead>
        <tbody>
          {inv.items.map(x => (
            <tr key={x.id}>
              <td>{x.name}</td><td>{x.unit ? `${x.quantity} ${x.unit}` : ''}</td>
              <td>{vnd(x.unitPrice)}</td><td className="num">{vnd(x.amount)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <div style={{ textAlign: 'right', fontWeight: 700, fontSize: 16, margin: '8px 0' }}>Tổng: {vnd(inv.totalAmount)}</div>

      <h4>Lịch sử thanh toán</h4>
      {inv.payments.length ? (
        <table>
          <thead><tr>
            <th>Ngày</th><th className="num">Số tiền</th><th>Phương thức</th><th>Trạng thái</th>
            <th>Mã giao dịch</th><th style={{ width: 170 }}></th>
          </tr></thead>
          <tbody>
            {inv.payments.map(p => (
              <tr key={p.id}>
                <td>{fmtDate(p.paidAt)}</td><td className="num">{vnd(p.amount)}</td>
                <td>{p.methodName}</td>
                <td><StatusBadge statusName={p.statusName} /></td>
                <td>{p.reference || '—'}</td>
                <td style={{ whiteSpace: 'nowrap' }}>
                  {p.status === 'Pending' && (
                    <>
                      <button className="btn ok sm" onClick={() => confirmPayment(p.id)}>Xác nhận</button>{' '}
                      <button className="btn danger sm" onClick={() => rejectPayment(p.id)}>Từ chối</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : <div className="empty" style={{ padding: 10 }}>Chưa có thanh toán</div>}

      {inv.pendingAmount > 0 && (
        <div className="muted small" style={{ marginTop: 8 }}>
          Đang chờ chủ trọ xác nhận <b>{vnd(inv.pendingAmount)}</b> do người thuê báo đã trả.
        </div>
      )}

      {rem > 0 ? (
        <div style={{ marginTop: 14, borderTop: '1px solid var(--line)', paddingTop: 10 }}>
          <DetailRow label="Còn phải thu" value={<b>{vnd(rem)}</b>} />
          <div className="form-grid">
            <div><label>Số tiền</label><input type="number" value={amount} onChange={e => setAmount(e.target.value)} /></div>
            <div>
              <label>Phương thức</label>
              <select value={method} onChange={e => setMethod(e.target.value as PaymentMethod)}>
                {PAYMENT_METHODS.map(m => <option key={m} value={m}>{paymentMethodName(m)}</option>)}
              </select>
            </div>
            <div style={{ gridColumn: '1/-1' }}><label>Ghi chú</label><input value={note} onChange={e => setNote(e.target.value)} /></div>
          </div>
          <div className="form-actions">
            <button className="btn ok" onClick={pay} disabled={busy}>Ghi nhận thu tiền</button>
          </div>
        </div>
      ) : <div className="muted small" style={{ marginTop: 8 }}>✅ Đã thanh toán đủ.</div>}
    </Modal>
  )
}
