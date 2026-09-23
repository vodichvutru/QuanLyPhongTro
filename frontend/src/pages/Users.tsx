import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, Badge, Modal, EmptyRow, Loading } from '../lib/ui'
import { roleName } from '../lib/helpers'
import type { RoleDto, UserDto } from '../lib/types'

function checkPass(a: string, b: string, toast: (m: string, k?: 'ok' | 'err') => void): string | null {
  if (!a) { toast('Nhập mật khẩu.', 'err'); return null }
  if (a.length < 6) { toast('Mật khẩu phải có ít nhất 6 ký tự.', 'err'); return null }
  if (a !== b) { toast('Mật khẩu nhập lại không khớp.', 'err'); return null }
  return a
}

export default function Users() {
  const toast = useToast()
  const [users, setUsers] = useState<UserDto[]>([])
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)
  const [rolesFor, setRolesFor] = useState<UserDto | null>(null)
  const [pwFor, setPwFor] = useState<UserDto | null>(null)

  const load = async () => {
    setLoading(true)
    try {
      const [u, r] = await Promise.all([api.get<UserDto[]>('/admin/users'), api.get<RoleDto[]>('/admin/roles')])
      setUsers(u); setRoles(r)
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const toggle = async (u: UserDto) => {
    try { await api.put(`/admin/users/${u.id}/active`, { isActive: !u.isActive }); toast('Đã cập nhật'); load() }
    catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <>
      <PageHead title="Quản lý người dùng" actions={<button className="btn" onClick={() => setCreating(true)}>+ Tạo người dùng</button>} />
      <div className="muted small" style={{ marginBottom: 10 }}>
        Tạo tài khoản <b>Chủ trọ</b> tại đây. Tài khoản <b>Người thuê</b> được tạo gắn với hồ sơ ở trang <b>Người thuê</b>.
      </div>
      <div className="card">
        <table>
          <thead><tr><th>Username</th><th>Họ tên</th><th>Vai trò</th><th>Trạng thái</th><th style={{ width: 300 }}></th></tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={5}><Loading /></td></tr>
              : users.length ? users.map(u => (
                <tr key={u.id}>
                  <td><b>{u.username}</b></td>
                  <td>{u.fullName}</td>
                  <td>{(u.roles || []).map(r => <span key={r} style={{ marginRight: 4 }}><Badge text={roleName(r)} kind="brand" /></span>)}</td>
                  <td>{u.isActive ? <Badge text="Hoạt động" kind="ok" /> : <Badge text="Bị khóa" kind="bad" />}</td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    <button className="btn gray sm" onClick={() => setPwFor(u)}>Đặt lại MK</button>{' '}
                    <button className="btn gray sm" onClick={() => setRolesFor(u)}>Vai trò</button>{' '}
                    <button className={'btn ' + (u.isActive ? 'danger' : 'ok') + ' sm'} onClick={() => toggle(u)}>
                      {u.isActive ? 'Khóa' : 'Mở khóa'}
                    </button>
                  </td>
                </tr>
              )) : <EmptyRow cols={5} />}
          </tbody>
        </table>
      </div>

      {creating && <CreateUserModal roles={roles} onClose={() => setCreating(false)} onSaved={() => { setCreating(false); load() }} />}
      {rolesFor && <RolesModal user={rolesFor} roles={roles} onClose={() => setRolesFor(null)} onSaved={() => { setRolesFor(null); load() }} />}
      {pwFor && <UserPwModal user={pwFor} onClose={() => setPwFor(null)} onSaved={() => { setPwFor(null); load() }} />}
    </>
  )
}

function CreateUserModal({ roles, onClose, onSaved }: { roles: RoleDto[]; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const creatable = roles.filter(r => r.code !== 'Tenant')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [sel, setSel] = useState<string[]>(['Owner'])
  const [busy, setBusy] = useState(false)

  const toggleRole = (code: string) => setSel(a => a.includes(code) ? a.filter(x => x !== code) : [...a, code])

  const submit = async () => {
    if (password.length < 6) { toast('Mật khẩu phải có ít nhất 6 ký tự.', 'err'); return }
    setBusy(true)
    try {
      await api.post('/admin/users', {
        username, password, fullName, roles: sel.length ? sel : ['Owner'],
      })
      toast('Đã tạo'); onSaved()
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title="Tạo người dùng mới" onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn" onClick={submit} disabled={busy}>Tạo</button>
      </>}>
      <div className="form-grid">
        <div><label>Username *</label><input value={username} onChange={e => setUsername(e.target.value)} /></div>
        <div><label>Mật khẩu *</label><input type="password" value={password} onChange={e => setPassword(e.target.value)} /></div>
        <div><label>Họ tên *</label><input value={fullName} onChange={e => setFullName(e.target.value)} /></div>
      </div>
      <label>Vai trò</label>
      <div>
        {(creatable.length ? creatable : [{ id: 0, code: 'Owner', name: 'Chủ trọ', description: null }]).map(r => (
          <label key={r.code} className="check-row">
            <input type="checkbox" checked={sel.includes(r.code)} onChange={() => toggleRole(r.code)} /> {r.name}
          </label>
        ))}
      </div>
      <div className="muted small">Tài khoản Người thuê được tạo ở trang Người thuê (luôn gắn với hồ sơ).</div>
    </Modal>
  )
}

function RolesModal({ user, roles, onClose, onSaved }: { user: UserDto; roles: RoleDto[]; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [sel, setSel] = useState<string[]>(user.roles || [])
  const [busy, setBusy] = useState(false)
  const toggleRole = (code: string) => setSel(a => a.includes(code) ? a.filter(x => x !== code) : [...a, code])

  const submit = async () => {
    setBusy(true)
    try { await api.put(`/admin/users/${user.id}/roles`, { roles: sel }); toast('Đã lưu vai trò'); onSaved() }
    catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={`Gán vai trò: ${user.username}`} onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn" onClick={submit} disabled={busy}>Lưu</button>
      </>}>
      {roles.map(r => (
        <label key={r.code} className="check-row">
          <input type="checkbox" checked={sel.includes(r.code)} onChange={() => toggleRole(r.code)} /> {r.name}
        </label>
      ))}
    </Modal>
  )
}

function UserPwModal({ user, onClose, onSaved }: { user: UserDto; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [p1, setP1] = useState(''); const [p2, setP2] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    const pw = checkPass(p1, p2, toast)
    if (!pw) return
    setBusy(true)
    try { await api.put(`/admin/users/${user.id}/password`, { newPassword: pw }); toast('Đã đặt lại mật khẩu'); onSaved() }
    catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={`Đặt lại mật khẩu: ${user.username}`} onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn" onClick={submit} disabled={busy}>Lưu</button>
      </>}>
      <label>Mật khẩu mới * (tối thiểu 6 ký tự)</label>
      <input type="password" value={p1} onChange={e => setP1(e.target.value)} />
      <label>Nhập lại mật khẩu *</label>
      <input type="password" value={p2} onChange={e => setP2(e.target.value)} />
    </Modal>
  )
}
