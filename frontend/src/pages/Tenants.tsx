import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../lib/api'
import { useToast, PageHead, Badge, Modal, EmptyRow, Loading } from '../lib/ui'
import type { TenantDto } from '../lib/types'

export default function Tenants() {
  const toast = useToast()
  const [list, setList] = useState<TenantDto[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<TenantDto | null | undefined>(undefined)
  const [accFor, setAccFor] = useState<TenantDto | null>(null)
  const [pwFor, setPwFor] = useState<TenantDto | null>(null)

  const load = async () => {
    setLoading(true)
    try {
      setList(await api.get<TenantDto[]>('/tenants'))
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const del = async (t: TenantDto) => {
    if (!confirm('Xóa người thuê này?')) return
    try { await api.del('/tenants/' + t.id); toast('Đã xóa'); load() }
    catch (e) { toast((e as Error).message, 'err') }
  }

  return (
    <>
      <PageHead title="Người thuê" actions={<button className="btn" onClick={() => setEditing(null)}>+ Thêm người thuê</button>} />
      <div className="card">
        <table>
          <thead><tr>
            <th>Họ tên</th><th>SĐT</th><th>CCCD</th><th>Email</th><th>Trạng thái</th><th style={{ width: 280 }}></th>
          </tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={6}><Loading /></td></tr>
              : list.length ? list.map(t => (
                <tr key={t.id}>
                  <td>
                    <b>{t.fullName}</b><br />
                    <span className="small">{t.hasAccount ? <Badge text="có tài khoản" kind="ok" /> : <Badge text="chưa có tài khoản" kind="warn" />}</span>
                  </td>
                  <td>{t.phone || '—'}</td>
                  <td>{t.identityNumber || '—'}</td>
                  <td>{t.email || '—'}</td>
                  <td>{t.isActive ? <Badge text="Hoạt động" kind="ok" /> : <Badge text="Ngưng" kind="bad" />}</td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    {t.hasAccount
                      ? <button className="btn gray sm" onClick={() => setPwFor(t)}>Đặt lại MK</button>
                      : <button className="btn sm" onClick={() => setAccFor(t)}>+ Tạo tài khoản</button>}{' '}
                    <button className="btn gray sm" onClick={() => setEditing(t)}>Sửa</button>{' '}
                    <button className="btn danger sm" onClick={() => del(t)}>Xóa</button>
                  </td>
                </tr>
              )) : <EmptyRow cols={6} />}
          </tbody>
        </table>
      </div>

      {editing !== undefined && (
        <TenantForm tenant={editing}
          onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); load() }} />
      )}
      {accFor && <AccountModal tenant={accFor} onClose={() => setAccFor(null)} onSaved={() => { setAccFor(null); load() }} />}
      {pwFor && <PasswordModal tenant={pwFor} onClose={() => setPwFor(null)} onSaved={() => { setPwFor(null); load() }} />}
    </>
  )
}

function PasswordPair({ prefix, value1, value2, onChange1, onChange2, label = 'Mật khẩu mới *' }: {
  prefix: string; value1: string; value2: string
  onChange1: (v: string) => void; onChange2: (v: string) => void; label?: string
}) {
  return (
    <>
      <label>{label} (tối thiểu 6 ký tự)</label>
      <input id={prefix + '_p1'} type="password" value={value1} onChange={e => onChange1(e.target.value)} />
      <label>Nhập lại mật khẩu *</label>
      <input id={prefix + '_p2'} type="password" value={value2} onChange={e => onChange2(e.target.value)} />
    </>
  )
}

/** Kiểm tra 2 ô mật khẩu; trả về mật khẩu hoặc null (kèm toast lỗi). */
function checkPass(a: string, b: string, toast: (m: string, k?: 'ok' | 'err') => void): string | null {
  if (!a) { toast('Nhập mật khẩu.', 'err'); return null }
  if (a.length < 6) { toast('Mật khẩu phải có ít nhất 6 ký tự.', 'err'); return null }
  if (a !== b) { toast('Mật khẩu nhập lại không khớp.', 'err'); return null }
  return a
}

function TenantForm({ tenant, onClose, onSaved }: {
  tenant: TenantDto | null; onClose: () => void; onSaved: () => void
}) {
  const toast = useToast()
  const isEdit = !!tenant
  const [fullName, setFullName] = useState(tenant?.fullName || '')
  const [phone, setPhone] = useState(tenant?.phone || '')
  const [idn, setIdn] = useState(tenant?.identityNumber || '')
  const [email, setEmail] = useState(tenant?.email || '')
  const [address, setAddress] = useState(tenant?.address || '')
  const [note, setNote] = useState(tenant?.note || '')
  const [isActive, setIsActive] = useState(tenant?.isActive ?? true)
  const [withAccount, setWithAccount] = useState(false)
  const [username, setUsername] = useState('')
  const [p1, setP1] = useState(''); const [p2, setP2] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const body: Record<string, unknown> = {
      fullName, phone: phone || null, identityNumber: idn || null, email: email || null,
      address: address || null, note: note || null,
    }
    if (isEdit) {
      body.isActive = isActive
    } else {
      if (withAccount) {
        const pw = checkPass(p1, p2, toast)
        if (!pw) return
        body.username = username; body.password = pw
      }
    }
    setBusy(true)
    try {
      if (isEdit) await api.put('/tenants/' + tenant!.id, body)
      else await api.post('/tenants', body)
      toast('Đã lưu'); onSaved()
    } catch (err) { toast((err as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={isEdit ? 'Sửa người thuê' : 'Thêm người thuê'} onClose={onClose}
      footer={<>
        <button className="btn gray" type="button" onClick={onClose}>Đóng</button>
        <button className="btn" type="submit" form="tenant-form" disabled={busy}>Lưu</button>
      </>}>
      <form id="tenant-form" className="form-grid" onSubmit={submit}>
        <div style={{ gridColumn: '1/-1' }}><label>Họ tên *</label><input value={fullName} onChange={e => setFullName(e.target.value)} /></div>
        <div><label>SĐT</label><input value={phone} onChange={e => setPhone(e.target.value)} /></div>
        <div><label>CCCD/CMND</label><input value={idn} onChange={e => setIdn(e.target.value)} /></div>
        <div><label>Email</label><input value={email} onChange={e => setEmail(e.target.value)} /></div>
        <div><label>Địa chỉ</label><input value={address} onChange={e => setAddress(e.target.value)} /></div>
        <div style={{ gridColumn: '1/-1' }}><label>Ghi chú</label><input value={note} onChange={e => setNote(e.target.value)} /></div>
        {isEdit && (
          <div>
            <label>Hoạt động</label>
            <select value={String(isActive)} onChange={e => setIsActive(e.target.value === 'true')}>
              <option value="true">Hoạt động</option><option value="false">Ngưng</option>
            </select>
          </div>
        )}
      </form>
      {!isEdit && (
        <div style={{ marginTop: 14 }}>
          <label className="check-row">
            <input type="checkbox" checked={withAccount} onChange={e => setWithAccount(e.target.checked)} />
            Kèm tạo tài khoản đăng nhập cho người thuê này
          </label>
          {withAccount && (
            <div style={{ marginTop: 10, paddingTop: 10, borderTop: '1px dashed #ddd' }}>
              <label>Tên đăng nhập *</label><input value={username} onChange={e => setUsername(e.target.value)} />
              <PasswordPair prefix="tacc" value1={p1} value2={p2} onChange1={setP1} onChange2={setP2} />
              <div className="muted small">Gợi ý: đặt theo tên không dấu, VD tranvanh.</div>
            </div>
          )}
        </div>
      )}
    </Modal>
  )
}

function AccountModal({ tenant, onClose, onSaved }: { tenant: TenantDto; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [username, setUsername] = useState('')
  const [p1, setP1] = useState(''); const [p2, setP2] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    const pw = checkPass(p1, p2, toast)
    if (!pw) return
    setBusy(true)
    try {
      await api.post(`/tenants/${tenant.id}/account`, { username, password: pw })
      toast('Đã tạo tài khoản'); onSaved()
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={`Tạo tài khoản: ${tenant.fullName}`} onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn" onClick={submit} disabled={busy}>Tạo</button>
      </>}>
      <div className="muted small" style={{ margin: '0 0 10px' }}>
        Người thuê đăng nhập bằng tài khoản này để xem hợp đồng, hóa đơn và gửi yêu cầu sửa chữa.
      </div>
      <label>Tên đăng nhập *</label><input value={username} onChange={e => setUsername(e.target.value)} />
      <PasswordPair prefix="ta" value1={p1} value2={p2} onChange1={setP1} onChange2={setP2} />
    </Modal>
  )
}

function PasswordModal({ tenant, onClose, onSaved }: { tenant: TenantDto; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const [p1, setP1] = useState(''); const [p2, setP2] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    const pw = checkPass(p1, p2, toast)
    if (!pw) return
    setBusy(true)
    try {
      await api.put(`/tenants/${tenant.id}/password`, { newPassword: pw })
      toast('Đã đặt lại mật khẩu'); onSaved()
    } catch (e) { toast((e as Error).message, 'err') }
    finally { setBusy(false) }
  }

  return (
    <Modal title={`Đặt lại mật khẩu: ${tenant.fullName}`} onClose={onClose}
      footer={<>
        <button className="btn gray" onClick={onClose}>Đóng</button>
        <button className="btn" onClick={submit} disabled={busy}>Lưu</button>
      </>}>
      <div className="muted small" style={{ margin: '0 0 10px' }}>Đặt mật khẩu mới cho tài khoản đăng nhập của người thuê này.</div>
      <PasswordPair prefix="tp" value1={p1} value2={p2} onChange1={setP1} onChange2={setP2} />
    </Modal>
  )
}
