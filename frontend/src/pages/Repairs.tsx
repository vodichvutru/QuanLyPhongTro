import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, StatusBadge, Modal, DetailRow, EmptyRow, Loading } from '../lib/ui'
import { vnd, fmtDate, repairStatusName, REPAIR_STATUSES } from '../lib/helpers'
import type { RepairRequestDto, RepairStatus } from '../lib/types'

export default function Repairs() {
  const toast = useToast()
  const [status, setStatus] = useState('')
  const [list, setList] = useState<RepairRequestDto[]>([])
  const [loading, setLoading] = useState(true)
  const [detail, setDetail] = useState<RepairRequestDto | null>(null)
  const [updating, setUpdating] = useState<RepairRequestDto | null>(null)

  const load = async () => {
    setLoading(true)
    try {
      const qs = status ? '?status=' + encodeURIComponent(status) : ''
      setList(await api.get<RepairRequestDto[]>('/repair-requests' + qs))
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [status]) // eslint-disable-line react-hooks/exhaustive-deps

  const openDetail = async (id: number) => {
    try { setDetail(await api.get<RepairRequestDto>('/repair-requests/' + id)) }
    catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <>
      <PageHead title="Yêu cầu sửa chữa" />
      <div className="muted small" style={{ marginBottom: 10 }}>
        Chủ trọ <b>nhận và xử lý</b> yêu cầu do người thuê gửi (người thuê tạo ở cổng tự phục vụ của họ).
      </div>
      <div className="filters">
        <select style={{ width: 'auto' }} value={status} onChange={e => setStatus(e.target.value)}>
          <option value="">Tất cả trạng thái</option>
          {REPAIR_STATUSES.map(s => <option key={s} value={s}>{repairStatusName(s)}</option>)}
        </select>
      </div>
      <div className="card">
        <table>
          <thead><tr>
            <th>Phòng</th><th>Tiêu đề</th><th>Người thuê</th><th>Người tạo</th><th>Ngày</th><th>Trạng thái</th><th style={{ width: 170 }}></th>
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={7}><Loading /></td></tr>
              : list.length ? list.map(r => (
                <tr key={r.id}>
                  <td><b>{r.roomName}</b></td><td>{r.subject}</td>
                  <td>{r.tenantName || '—'}</td><td>{r.createdByName || '—'}</td>
                  <td className="small">{fmtDate(r.createdAt)}</td>
                  <td><StatusBadge statusName={r.statusName} /></td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    <button className="btn gray sm" onClick={() => openDetail(r.id)}>Xem</button>{' '}
                    {r.status !== 'Completed' && r.status !== 'Cancelled' &&
                      <button className="btn ok sm" onClick={() => setUpdating(r)}>Xử lý</button>}
                  </td>
                </tr>
              )) : <EmptyRow cols={7} />}
          </tbody>
        </table>
      </div>

      {detail && (
        <Modal title={`Yêu cầu #${detail.id}`} onClose={() => setDetail(null)}
          footer={<button className="btn gray" onClick={() => setDetail(null)}>Đóng</button>}>
          <div style={{ margin: '10px 0' }}>
            <DetailRow label="Phòng" value={detail.roomName} />
            <DetailRow label="Người thuê" value={detail.tenantName || '—'} />
            <DetailRow label="Người tạo" value={detail.createdByName || '—'} />
            <DetailRow label="Ngày gửi" value={fmtDate(detail.createdAt)} />
            <DetailRow label="Trạng thái" value={<StatusBadge statusName={detail.statusName} />} />
            <DetailRow label="Mô tả" value={detail.description || '—'} />
            <DetailRow label="Phản hồi" value={detail.ownerNote || '—'} />
            <DetailRow label="Chi phí" value={detail.cost != null ? vnd(detail.cost) : '—'} />
          </div>
        </Modal>
      )}

      {updating && <UpdateModal repair={updating} onClose={() => setUpdating(null)} onSaved={() => { setUpdating(null); load() }} />}
    </>
  )
}

function UpdateModal({ repair, onClose, onSaved }: { repair: RepairRequestDto; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [status, setStatus] = useState<RepairStatus>('Approved')
  const [note, setNote] = useState('')
  const [cost, setCost] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    setBusy(true)
    try {
      const c = parseFloat(cost)
      await api.put(`/repair-requests/${repair.id}/status`, {
        status, ownerNote: note || null, cost: isNaN(c) ? null : c,
      })
      toast('Đã cập nhật'); onSaved()
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={`Xử lý: ${repair.subject}`} onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn ok" onClick={submit} disabled={busy}>Cập nhật</button>
      </>}>
      <label>Trạng thái</label>
      <select value={status} onChange={e => setStatus(e.target.value as RepairStatus)}>
        {(['Approved', 'InProgress', 'Completed', 'Rejected'] as RepairStatus[]).map(s =>
          <option key={s} value={s}>{repairStatusName(s)}</option>)}
      </select>
      <label>Phản hồi / ghi chú</label>
      <textarea rows={2} value={note} onChange={e => setNote(e.target.value)} />
      <label>Chi phí (₫) — nếu có</label>
      <input type="number" value={cost} onChange={e => setCost(e.target.value)} />
    </Modal>
  )
}
