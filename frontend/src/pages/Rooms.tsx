import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, StatusBadge, Modal, EmptyRow, Loading } from '../lib/ui'
import { vnd, roomStatusName } from '../lib/helpers'
import type { RoomDto, RoomStatus } from '../lib/types'

const STATUSES: RoomStatus[] = ['Available', 'Rented', 'Maintenance']

export default function Rooms() {
  const toast = useToast()
  const [rooms, setRooms] = useState<RoomDto[]>([])
  const [loading, setLoading] = useState(true)
  // undefined = đóng modal, null = thêm mới, RoomDto = sửa
  const [editing, setEditing] = useState<RoomDto | null | undefined>(undefined)

  const load = async () => {
    setLoading(true)
    try {
      setRooms(await api.get<RoomDto[]>('/rooms'))
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const del = async (id: number) => {
    if (!confirm('Xóa phòng này?')) return
    try { await api.del('/rooms/' + id); toast('Đã xóa'); load() }
    catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <>
      <PageHead title="Phòng trọ" actions={<button className="btn" onClick={() => setEditing(null)}>+ Thêm phòng</button>} />
      <div className="card">
        <table>
          <thead><tr>
            <th>Phòng</th><th>Tầng</th><th className="num">Diện tích</th><th className="num">Giá/tháng</th>
            <th>SL tối đa</th><th>Trạng thái</th><th>Người thuê</th><th style={{ width: 120 }}></th>
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={8}><Loading /></td></tr>
              : rooms.length ? rooms.map(r => (
                <tr key={r.id}>
                  <td><b>{r.name}</b></td>
                  <td>{r.floor || '—'}</td>
                  <td className="num">{r.area} m²</td>
                  <td className="num">{vnd(r.price)}</td>
                  <td>{r.maxPeople}</td>
                  <td><StatusBadge statusName={r.statusName} /></td>
                  <td>{r.tenantName || '—'}</td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    <button className="btn gray sm" onClick={() => setEditing(r)}>Sửa</button>{' '}
                    <button className="btn danger sm" onClick={() => del(r.id)}>Xóa</button>
                  </td>
                </tr>
              )) : <EmptyRow cols={8} />}
          </tbody>
        </table>
      </div>

      {editing !== undefined && (
        <RoomForm
          room={editing}
          onClose={() => setEditing(undefined)}
          onSaved={() => { setEditing(undefined); load() }}
        />
      )}
    </>
  )
}

function RoomForm({ room, onClose, onSaved }: {
  room: RoomDto | null; onClose: () => void; onSaved: () => void
}) {
  const toast = useToast()
  const isEdit = !!room
  const [name, setName] = useState(room?.name || '')
  const [floor, setFloor] = useState(room?.floor || '')
  const [area, setArea] = useState(room ? String(room.area) : '')
  const [price, setPrice] = useState(room ? String(room.price) : '')
  const [maxPeople, setMaxPeople] = useState(room ? String(room.maxPeople) : '2')
  const [note, setNote] = useState(room?.note || '')
  const [status, setStatus] = useState<RoomStatus>(room?.status || 'Available')
  const [busy, setBusy] = useState(false)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const body: Record<string, unknown> = {
      name, floor: floor || null, area: Number(area), price: Number(price),
      maxPeople: Number(maxPeople) || 2, note: note || null,
    }
    if (isEdit) body.status = status
    setBusy(true)
    try {
      if (isEdit) await api.put('/rooms/' + room!.id, body)
      else await api.post('/rooms', body)
      toast('Đã lưu phòng'); onSaved()
    } catch (err) { toast((err as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal
      title={isEdit ? `Sửa phòng ${room!.name}` : 'Thêm phòng mới'}
      onClose={onClose}
      footer={<>
        <button className="btn gray" type="button" onClick={onClose}>Đóng</button>
        <button className="btn" type="submit" form="room-form" disabled={busy}>Lưu</button>
      </>}
    >
      <form id="room-form" className="form-grid" onSubmit={submit}>
        <div><label>Mã phòng</label><input value={name} onChange={e => setName(e.target.value)} /></div>
        <div><label>Tầng</label><input value={floor} onChange={e => setFloor(e.target.value)} /></div>
        <div><label>Diện tích (m²)</label><input type="number" value={area} onChange={e => setArea(e.target.value)} /></div>
        <div><label>Giá phòng (₫/tháng)</label><input type="number" value={price} onChange={e => setPrice(e.target.value)} /></div>
        <div><label>SL người tối đa</label><input type="number" value={maxPeople} onChange={e => setMaxPeople(e.target.value)} /></div>
        {isEdit && (
          <div>
            <label>Trạng thái</label>
            <select value={status} onChange={e => setStatus(e.target.value as RoomStatus)}>
              {STATUSES.map(s => <option key={s} value={s}>{roomStatusName(s)}</option>)}
            </select>
          </div>
        )}
        <div style={{ gridColumn: '1/-1' }}><label>Ghi chú</label><input value={note} onChange={e => setNote(e.target.value)} /></div>
      </form>
    </Modal>
  )
}
