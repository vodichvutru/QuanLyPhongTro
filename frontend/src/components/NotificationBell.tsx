import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api'
import { useToast } from '../lib/ui'
import { fmtDate } from '../lib/helpers'
import type { NotificationDto } from '../lib/types'

/** Chuông thông báo trên thanh trên: đếm chưa đọc + xem danh sách + bấm để mở trang liên quan. */
export default function NotificationBell() {
  const toast = useToast()
  const nav = useNavigate()
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<NotificationDto[]>([])
  const [count, setCount] = useState(0)
  const boxRef = useRef<HTMLDivElement>(null)

  const loadCount = async () => {
    try { setCount(await api.get<number>('/notifications/unread-count')) } catch { /* im lặng */ }
  }
  const loadList = async () => {
    try { setItems(await api.get<NotificationDto[]>('/notifications')) }
    catch (e) { toast((e as Error).message, 'err') }
  }

  useEffect(() => {
    loadCount()
    const t = setInterval(loadCount, 20000) // làm mới số chưa đọc mỗi 20 giây
    return () => clearInterval(t)
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  // đóng panel khi bấm ra ngoài
  useEffect(() => {
    const onDoc = (e: MouseEvent) => {
      if (boxRef.current && !boxRef.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onDoc)
    return () => document.removeEventListener('mousedown', onDoc)
  }, [])

  const toggle = async () => {
    const next = !open
    setOpen(next)
    if (next) { await loadList(); await loadCount() }
  }

  const openItem = async (n: NotificationDto) => {
    try { if (!n.isRead) await api.post(`/notifications/${n.id}/read`) } catch { /* im lặng */ }
    setOpen(false)
    if (n.linkPath) nav(n.linkPath)
    loadCount()
  }

  const markAll = async () => {
    try {
      await api.post('/notifications/read-all')
      await loadList(); await loadCount()
    } catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <div className="notif" ref={boxRef}>
      <button className="btn ghost sm notif-btn" onClick={toggle} type="button" title="Thông báo">
        🔔{count > 0 && <span className="notif-badge">{count > 99 ? '99+' : count}</span>}
      </button>
      {open && (
        <div className="notif-panel">
          <div className="notif-head">
            <b>Thông báo</b>
            {count > 0 && <button className="link" onClick={markAll} type="button">Đánh dấu đã đọc</button>}
          </div>
          {items.length ? items.map(n => (
            <button key={n.id} className={'notif-item' + (n.isRead ? '' : ' unread')} onClick={() => openItem(n)} type="button">
              <div className="t">{n.title}</div>
              <div className="m">{n.message}</div>
              <div className="d">{fmtDate(n.createdAt)}</div>
            </button>
          )) : <div className="empty" style={{ padding: 18 }}>Không có thông báo</div>}
        </div>
      )}
    </div>
  )
}
