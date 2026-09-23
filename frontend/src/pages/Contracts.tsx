import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, StatusBadge, Modal, DetailRow, EmptyRow, Loading } from '../lib/ui'
import { vnd, fmtDate, dateInputVal } from '../lib/helpers'
import type { ContractDto, RoomDto, TenantDto } from '../lib/types'

export default function Contracts() {
  const toast = useToast()
  const [list, setList] = useState<ContractDto[]>([])
  const [loading, setLoading] = useState(true)
  const [view, setView] = useState<ContractDto | null>(null)
  const [creating, setCreating] = useState(false)
  const [term, setTerm] = useState<ContractDto | null>(null)

  const load = async () => {
    setLoading(true)
    try { setList(await api.get<ContractDto[]>('/contracts')) }
    catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const openView = async (id: number) => {
    try { setView(await api.get<ContractDto>('/contracts/' + id)) }
    catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <>
      <PageHead title="Hợp đồng thuê" actions={<button className="btn" onClick={() => setCreating(true)}>+ Lập hợp đồng</button>} />
      <div className="card">
        <table>
          <thead><tr>
            <th>Mã HĐ</th><th>Phòng</th><th>Người thuê</th><th>Thời hạn</th>
            <th className="num">Tiền phòng</th><th>Trạng thái</th><th style={{ width: 170 }}></th>
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={7}><Loading /></td></tr>
              : list.length ? list.map(c => (
                <tr key={c.id}>
                  <td>{c.contractCode}</td>
                  <td><b>{c.roomName}</b></td>
                  <td>{c.tenantName}</td>
                  <td className="small">{fmtDate(c.startDate)} → {fmtDate(c.endDate)}</td>
                  <td className="num">{vnd(c.monthlyRent)}</td>
                  <td><StatusBadge statusName={c.statusName} /></td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    <button className="btn gray sm" onClick={() => openView(c.id)}>Xem</button>{' '}
                    {c.status === 'Active' && <button className="btn danger sm" onClick={() => setTerm(c)}>Chấm dứt</button>}
                  </td>
                </tr>
              )) : <EmptyRow cols={7} />}
          </tbody>
        </table>
      </div>

      {view && (
        <Modal title={`Hợp đồng ${view.contractCode}`} onClose={() => setView(null)}
          footer={<button className="btn gray" onClick={() => setView(null)}>Đóng</button>}>
          <div style={{ margin: '10px 0' }}>
            <DetailRow label="Phòng" value={view.roomName} />
            <DetailRow label="Người thuê" value={view.tenantName} />
            <DetailRow label="Thời hạn" value={`${fmtDate(view.startDate)} → ${fmtDate(view.endDate)}`} />
            <DetailRow label="Tiền phòng / tháng" value={vnd(view.monthlyRent)} />
            <DetailRow label="Tiền đặt cọc" value={vnd(view.deposit)} />
            <DetailRow label="Giá điện" value={`${vnd(view.electricPrice)} / kWh`} />
            <DetailRow label="Giá nước" value={`${vnd(view.waterPrice)} / m³`} />
            <DetailRow label="Trạng thái" value={<StatusBadge statusName={view.statusName} />} />
            <DetailRow label="Ghi chú" value={view.note || '—'} />
          </div>
        </Modal>
      )}

      {creating && <ContractForm onClose={() => setCreating(false)} onSaved={() => { setCreating(false); load() }} />}
      {term && <TerminateModal contract={term} onClose={() => setTerm(null)} onSaved={() => { setTerm(null); load() }} />}
    </>
  )
}

function ContractForm({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [rooms, setRooms] = useState<RoomDto[]>([])
  const [tenants, setTenants] = useState<TenantDto[]>([])
  const [roomId, setRoomId] = useState('')
  const [tenantId, setTenantId] = useState('')
  const [start, setStart] = useState(dateInputVal(new Date().toISOString()))
  const [end, setEnd] = useState('')
  const [rent, setRent] = useState('2000000')
  const [dep, setDep] = useState('0')
  const [elec, setElec] = useState('3500')
  const [water, setWater] = useState('25000')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    (async () => {
      try {
        const [r, t] = await Promise.all([api.get<RoomDto[]>('/rooms'), api.get<TenantDto[]>('/tenants')])
        setRooms(r.filter(x => x.status === 'Available')); setTenants(t)
      } catch (e) { toast((e as Error).message, 'err') }
    })()
  }, [toast])

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!roomId || !tenantId) { toast('Chọn phòng và người thuê.', 'err'); return }
    setBusy(true)
    try {
      await api.post('/contracts', {
        roomId: Number(roomId), tenantId: Number(tenantId), startDate: start, endDate: end || null,
        monthlyRent: Number(rent), deposit: Number(dep), electricPrice: Number(elec), waterPrice: Number(water),
      })
      toast('Đã lập hợp đồng'); onSaved()
    } catch (err) { toast((err as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title="Lập hợp đồng mới" onClose={onClose}
      footer={<>
        <button className="btn gray" type="button" onClick={onClose}>Đóng</button>
        <button className="btn" type="submit" form="contract-form" disabled={busy}>Lưu hợp đồng</button>
      </>}>
      <form id="contract-form" className="form-grid" onSubmit={submit}>
        <div>
          <label>Phòng</label>
          <select value={roomId} onChange={e => setRoomId(e.target.value)}>
            <option value="">— Chọn phòng trống —</option>
            {rooms.map(r => <option key={r.id} value={r.id}>{r.name} ({vnd(r.price)}/tháng)</option>)}
          </select>
        </div>
        <div>
          <label>Người thuê</label>
          <select value={tenantId} onChange={e => setTenantId(e.target.value)}>
            <option value="">— Chọn người thuê —</option>
            {tenants.map(t => <option key={t.id} value={t.id}>{t.fullName}</option>)}
          </select>
        </div>
        <div><label>Ngày bắt đầu</label><input type="date" value={start} onChange={e => setStart(e.target.value)} /></div>
        <div><label>Ngày kết thúc (bỏ trống = mở)</label><input type="date" value={end} onChange={e => setEnd(e.target.value)} /></div>
        <div><label>Tiền phòng (₫/tháng)</label><input type="number" value={rent} onChange={e => setRent(e.target.value)} /></div>
        <div><label>Tiền đặt cọc</label><input type="number" value={dep} onChange={e => setDep(e.target.value)} /></div>
        <div><label>Giá điện (₫/kWh)</label><input type="number" value={elec} onChange={e => setElec(e.target.value)} /></div>
        <div><label>Giá nước (₫/m³)</label><input type="number" value={water} onChange={e => setWater(e.target.value)} /></div>
      </form>
    </Modal>
  )
}

function TerminateModal({ contract, onClose, onSaved }: { contract: ContractDto; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [date, setDate] = useState(dateInputVal(new Date().toISOString()))
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    setBusy(true)
    try {
      await api.post(`/contracts/${contract.id}/terminate`, { terminatedAt: date || null })
      toast('Đã chấm dứt hợp đồng'); onSaved()
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={`Chấm dứt hợp đồng ${contract.contractCode}`} onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn danger" onClick={submit} disabled={busy}>Chấm dứt</button>
      </>}>
      <label>Ngày chấm dứt</label>
      <input type="date" value={date} onChange={e => setDate(e.target.value)} />
    </Modal>
  )
}
