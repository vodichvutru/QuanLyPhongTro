import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { statusKind } from './helpers'

/* ---------- Toast ---------- */
type ToastKind = 'ok' | 'err'
interface ToastItem { id: number; msg: string; kind: ToastKind }
const ToastCtx = createContext<(msg: string, kind?: ToastKind) => void>(() => {})
let toastId = 0

export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([])
  const push = useCallback((msg: string, kind: ToastKind = 'ok') => {
    const id = ++toastId
    setItems(arr => [...arr, { id, msg, kind }])
    setTimeout(() => setItems(arr => arr.filter(t => t.id !== id)), 3200)
  }, [])
  return (
    <ToastCtx.Provider value={push}>
      {children}
      <div className="toast-root">
        {items.map(t => <div key={t.id} className={'toast ' + t.kind}>{t.msg}</div>)}
      </div>
    </ToastCtx.Provider>
  )
}
export const useToast = () => useContext(ToastCtx)

/* ---------- Modal ---------- */
export function Modal({ title, onClose, children, footer }: {
  title: string; onClose: () => void; children: ReactNode; footer?: ReactNode
}) {
  // Render qua portal ra body: tránh bị ảnh hưởng bởi transform của animation trang (#view.fx)
  return createPortal(
    <div className="modal-backdrop" onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="modal">
        <button className="x" onClick={onClose} type="button">✕</button>
        <h3>{title}</h3>
        <div>{children}</div>
        {footer && <div className="form-actions">{footer}</div>}
      </div>
    </div>,
    document.body,
  )
}

/* ---------- thành phần nhỏ ---------- */
export function Badge({ text, kind }: { text: string; kind?: string }) {
  return <span className={'badge ' + (kind || '')}>{text}</span>
}
export function StatusBadge({ statusName }: { statusName: string }) {
  return <Badge text={statusName} kind={statusKind(statusName)} />
}
export function PageHead({ title, actions }: { title: string; actions?: ReactNode }) {
  return <div className="page-head"><h1>{title}</h1><div>{actions}</div></div>
}
export interface KpiCard { t: string; v: ReactNode; s?: string }
export function Kpis({ cards }: { cards: KpiCard[] }) {
  return (
    <div className="kpis">
      {cards.map((c, i) => (
        <div className="kpi" key={i}>
          <div className="t">{c.t}</div>
          <div className="v">{c.v}</div>
          {c.s && <div className="s">{c.s}</div>}
        </div>
      ))}
    </div>
  )
}
export function EmptyRow({ cols }: { cols: number }) {
  return <tr><td colSpan={cols} className="empty">Chưa có dữ liệu</td></tr>
}
export function Loading({ text = 'Đang tải…' }: { text?: string }) {
  return <div className="center muted" style={{ padding: 40 }}>{text}</div>
}
export function DetailRow({ label, value, total }: { label: string; value: ReactNode; total?: boolean }) {
  return <div className={'detail-row' + (total ? ' total' : '')}><span>{label}</span><span>{value}</span></div>
}
