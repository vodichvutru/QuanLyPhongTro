import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, StatusBadge, Modal, DetailRow, EmptyRow, Loading } from '../lib/ui'
import { vnd, fmtDate, paymentMethodName } from '../lib/helpers'
import type { InvoiceDto, PaymentMethod } from '../lib/types'

const PAY_METHODS: PaymentMethod[] = ['BankTransfer', 'Momo', 'VnPay']

export default function MyInvoices() {
  const toast = useToast()
  const [list, setList] = useState<InvoiceDto[]>([])
  const [loading, setLoading] = useState(true)
  const [detail, setDetail] = useState<InvoiceDto | null>(null)
  const [payFor, setPayFor] = useState<InvoiceDto | null>(null)

  const load = async () => {
    setLoading(true)
    try { setList(await api.get<InvoiceDto[]>('/me/invoices')) }
    catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const openDetail = async (id: number) => {
    try { setDetail(await api.get<InvoiceDto>('/me/invoices/' + id)) }
    catch (e) { toast((e as Error).message, 'err') }
  }

  const allPaid = list.length > 0 && list.every(x => x.totalAmount - x.paidAmount <= 0)

  return (
    <>
      <PageHead title="Hóa đơn của tôi" />
      <div className="card">
        <table>
          <thead><tr>
            <th>Mã</th><th>Phòng</th><th>Kỳ</th><th>Hạn</th>
            <th className="num">Tổng</th><th className="num">Đã trả</th><th className="num">Còn lại</th>
            <th>Trạng thái</th><th style={{ width: 200 }}></th>
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={9}><Loading /></td></tr>
              : list.length ? list.map(i => {
                const rem = Math.max(0, i.totalAmount - i.paidAmount - i.pendingAmount)
                return (
                  <tr key={i.id}>
                    <td>{i.invoiceCode}</td><td><b>{i.roomName}</b></td><td>{i.billingMonth}</td>
                    <td>{fmtDate(i.dueDate)}</td>
                    <td className="num">{vnd(i.totalAmount)}</td>
                    <td className="num">
                      {vnd(i.paidAmount)}
                      {i.pendingAmount > 0 && <div className="small" style={{ color: 'var(--warn)' }}>chờ xác nhận {vnd(i.pendingAmount)}</div>}
                    </td>
                    <td className="num"><b>{vnd(rem)}</b></td>
                    <td><StatusBadge statusName={i.statusName} /></td>
                    <td style={{ whiteSpace: 'nowrap' }}>
                      {rem > 0 && <button className="btn ok sm" onClick={() => setPayFor(i)}>Thanh toán</button>}{' '}
                      <button className="btn gray sm" onClick={() => openDetail(i.id)}>Chi tiết</button>
                    </td>
                  </tr>
                )
              }) : <EmptyRow cols={9} />}
          </tbody>
        </table>
        {allPaid && <div className="empty">Tất cả hóa đơn của bạn đã thanh toán đủ 🎉</div>}
      </div>

      {detail && (
        <Modal title={`Hóa đơn ${detail.invoiceCode}`} onClose={() => setDetail(null)}
          footer={detail.totalAmount - detail.paidAmount > 0
            ? <button className="btn ok" onClick={() => { const d = detail; setDetail(null); setPayFor(d) }}>
                Thanh toán {vnd(detail.totalAmount - detail.paidAmount)}
              </button>
            : <button className="btn gray" onClick={() => setDetail(null)}>Đóng</button>}>
          <div style={{ margin: '10px 0' }}>
            <DetailRow label="Phòng" value={detail.roomName} />
            <DetailRow label="Kỳ" value={detail.billingMonth} />
            <DetailRow label="Hạn" value={fmtDate(detail.dueDate)} />
            <DetailRow label="Trạng thái" value={<StatusBadge statusName={detail.statusName} />} />
          </div>
          <h4>Chi tiết</h4>
          <table>
            <thead><tr><th>Khoản</th><th className="num">Thành tiền</th></tr></thead>
            <tbody>
              {detail.items.map(x => (
                <tr key={x.id}>
                  <td>{x.name}{x.unit ? <span className="small muted"> ({x.quantity} {x.unit})</span> : null}</td>
                  <td className="num">{vnd(x.amount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ textAlign: 'right', fontWeight: 700, fontSize: 16, marginTop: 6 }}>Tổng: {vnd(detail.totalAmount)}</div>
          <h4>Thanh toán</h4>
          {detail.payments.length ? (
            <table>
              <thead><tr><th>Ngày</th><th className="num">Số tiền</th><th>Phương thức</th><th>Mã giao dịch</th><th>Ghi chú</th></tr></thead>
              <tbody>
                {detail.payments.map(p => (
                  <tr key={p.id}>
                    <td>{fmtDate(p.paidAt)}</td><td className="num">{vnd(p.amount)}</td>
                    <td>{p.methodName}</td><td>{p.reference || '—'}</td><td>{p.note || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : <div className="empty" style={{ padding: 10 }}>Chưa thanh toán</div>}
        </Modal>
      )}

      {payFor && <PayModal invoice={payFor} onClose={() => setPayFor(null)} onSaved={() => { setPayFor(null); load() }} />}
    </>
  )
}

function PayModal({ invoice, onClose, onSaved }: { invoice: InvoiceDto; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const rem = Math.max(0, invoice.totalAmount - invoice.paidAmount)
  const [method, setMethod] = useState<PaymentMethod>('BankTransfer')
  const [ref, setRef] = useState('')
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    setBusy(true)
    try {
      await api.post(`/me/invoices/${invoice.id}/pay`, { method, reference: ref || null, note: note || null })
      toast('Đã gửi xác nhận thanh toán — chờ chủ trọ kiểm tra và xác nhận.'); onSaved()
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={`Thanh toán hóa đơn ${invoice.invoiceCode}`} onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn ok" onClick={submit} disabled={busy}>Xác nhận thanh toán {vnd(rem)}</button>
      </>}>
      <div style={{ margin: '6px 0' }}>
        <DetailRow label="Phòng" value={invoice.roomName} />
        <DetailRow label="Kỳ" value={invoice.billingMonth} />
        <DetailRow label="Tổng hóa đơn" value={vnd(invoice.totalAmount)} />
        <DetailRow label="Đã thanh toán" value={vnd(invoice.paidAmount)} />
        <DetailRow label="Còn phải trả" value={<b>{vnd(rem)}</b>} />
      </div>
      <div className="muted small" style={{ margin: '4px 0 6px' }}>
        💳 Thanh toán trực tuyến (giả lập): ghi nhận ngay, Chủ trọ sẽ thấy khoản này để đối chiếu.
      </div>
      <label>Phương thức</label>
      <select value={method} onChange={e => setMethod(e.target.value as PaymentMethod)}>
        {PAY_METHODS.map(m => <option key={m} value={m}>{paymentMethodName(m)}</option>)}
      </select>
      <label>Mã giao dịch (bỏ trống cũng được)</label>
      <input value={ref} onChange={e => setRef(e.target.value)} />
      <label>Ghi chú (tuỳ chọn)</label>
      <input value={note} onChange={e => setNote(e.target.value)} />
    </Modal>
  )
}
