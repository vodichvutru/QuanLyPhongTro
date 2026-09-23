import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/auth'
import { useToast } from '../lib/ui'

export default function Login() {
  const { login } = useAuth()
  const toast = useToast()
  const nav = useNavigate()
  const [username, setUsername] = useState('chutro')
  const [password, setPassword] = useState('123456')
  const [busy, setBusy] = useState(false)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!username || !password) { toast('Nhập tên đăng nhập và mật khẩu.', 'err'); return }
    setBusy(true)
    try {
      await login(username, password)
      toast('Đăng nhập thành công!')
      nav('/home', { replace: true }) // luôn về trang chủ của vai trò mới (tránh giữ trang của tài khoản trước)
    } catch (err) {
      toast((err as Error).message, 'err')
    } finally { setBusy(false) }
  }

  return (
    <div className="login-wrap">
      <form className="login-card" onSubmit={submit}>
        <h1>🏠 Quản lý phòng trọ</h1>
        <div className="sub">Đăng nhập để tiếp tục</div>
        <label>Tên đăng nhập</label>
        <input value={username} onChange={e => setUsername(e.target.value)} autoComplete="username" />
        <label>Mật khẩu</label>
        <input type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete="current-password" />
        <div style={{ marginTop: 18 }}>
          <button className="btn" style={{ width: '100%', justifyContent: 'center' }} disabled={busy} type="submit">
            {busy ? 'Đang đăng nhập…' : 'Đăng nhập'}
          </button>
        </div>
        <div className="demo-hint">
          <b>Tài khoản demo:</b> chutro/123456 (Chủ trọ) · admin/123456 (Quản trị) · nguyenvana/123456 (Người thuê — tên = tài khoản)
        </div>
        <div className="muted small" style={{ textAlign: 'center' }}>Quên mật khẩu? Liên hệ Chủ trọ/Admin để đặt lại.</div>
      </form>
    </div>
  )
}
