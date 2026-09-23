import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, StatusBadge, Modal, EmptyRow, Loading } from '../lib/ui'
import { fmtDate } from '../lib/helpers'
import type { ContractDto, RepairRequestDto } from '../lib/types'

interface RoomOpt { id: number; name: string }

export default function MyRepairs() {
  const toast = useToast()
  const [list, setList] = useState<RepairRequestDto[]>([])
  const [rooms, setRooms] = useState<RoomOpt[]>([])
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const [list, cons] = await Promise.all([
        api.get<RepairRequestDto[]>('/me/repair-requests'),
        api.get<ContractDto[]>('/me/contracts'),
      ])
      setList(list)
      setRooms(cons.filter(c => c.status === 'Active').map(c => ({ id: c.roomId, name: c.roomName })))
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const cancel = async (id: number) => {
    if (!confirm('Hủy yêu cầu này?')) return
    try { await api.post(`/me/repair-requests/${id}/cancel`); toast('Đã hủy'); load() }
    catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <>
      <PageHead title="Yêu cầu sửa chữa" actions={<button className="btn" onClick={() => setCreating(true)}>+ Gửi yêu cầu</button>} />
      <div className="card">
        <table>
          <thead><tr><th>Phòng</th><th>Tiêu đề</th><th>Ngày gửi</th><th>Trạng thái</th><th style={{ width: 110 }}></th></tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={5}><Loading /></td></tr>
              : list.length ? list.map(r => (
                <tr key={r.id}>
                  <td><b>{r.roomName}</b></td><td>{r.subject}</td>
                  <td className="small">{fmtDate(r.createdAt)}</td>
                  <td><StatusBadge statusName={r.statusName} /></td>
                  <td>{r.status === 'Pending' && <button className="btn danger sm" onClick={() => cancel(r.id)}>Hủy</button>}</td>
                </tr>
              )) : <EmptyRow cols={5} />}
          </tbody>
        </table>
        {!rooms.length && <div className="empty">Bạn chưa có phòng nào trong hợp đồng.</div>}
      </div>

      {creating && <MyRepairForm rooms={rooms} onClose={() => setCreating(false)} onSaved={() => { setCreating(false); load() }} />}
    </>
  )
}

function MyRepairForm({ rooms, onClose, onSaved }: { rooms: RoomOpt[]; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [roomId, setRoomId] = useState(rooms.length ? String(rooms[0].id) : '')
  const [subject, setSubject] = useState('')
  const [desc, setDesc] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    try {
      await api.post('/me/repair-requests', { roomId: Number(roomId), subject, description: desc || null })
      toast('Đã gửi yêu cầu'); onSaved()
    } catch (err) { toast((err as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title="Gửi yêu cầu sửa chữa" onClose={onClose}
      footer={<>
        <button className="btn gray" type="button" onClick={onClose}>Đóng</button>
        <button className="btn" type="submit" form="myrepair-form" disabled={busy}>Gửi</button>
      </>}>
      <form id="myrepair-form" onSubmit={submit}>
        <label>Phòng</label>
        <select value={roomId} onChange={e => setRoomId(e.target.value)}>
          {rooms.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
        </select>
        <label>Tiêu đề *</label><input value={subject} onChange={e => setSubject(e.target.value)} />
        <label>Mô tả sự cố</label><textarea rows={2} value={desc} onChange={e => setDesc(e.target.value)} />
      </form>
    </Modal>
  )
}
