import type { ReactNode } from 'react'
import { HashRouter, NavLink, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './lib/auth'
import { ToastProvider } from './lib/ui'
import { roleName } from './lib/helpers'
import NotificationBell from './components/NotificationBell'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Rooms from './pages/Rooms'
import Tenants from './pages/Tenants'
import Contracts from './pages/Contracts'
import Invoices from './pages/Invoices'
import Repairs from './pages/Repairs'
import Reports from './pages/Reports'
import Users from './pages/Users'
import MyContracts from './pages/MyContracts'
import MyInvoices from './pages/MyInvoices'
import MyRepairs from './pages/MyRepairs'

/** Mỗi mục = 1 route + 1 mục menu; route nào vai trò không có quyền sẽ không được đăng ký → tự chuyển về /home. */
const PAGES: { path: string; label: string; tenantLabel?: string; roles: string[]; element: ReactNode }[] = [
  { path: '/home', label: 'Tổng quan', tenantLabel: 'Trang chủ', roles: ['Admin', 'Owner', 'Tenant'], element: <Dashboard /> },
  { path: '/rooms', label: 'Phòng trọ', roles: ['Admin', 'Owner'], element: <Rooms /> },
  { path: '/tenants', label: 'Người thuê', roles: ['Admin', 'Owner'], element: <Tenants /> },
  { path: '/contracts', label: 'Hợp đồng', roles: ['Admin', 'Owner'], element: <Contracts /> },
  { path: '/invoices', label: 'Hóa đơn', roles: ['Admin', 'Owner'], element: <Invoices /> },
  { path: '/repairs', label: 'Yêu cầu sửa chữa', roles: ['Admin', 'Owner'], element: <Repairs /> },
  { path: '/reports', label: 'Báo cáo', roles: ['Admin', 'Owner'], element: <Reports /> },
  { path: '/users', label: 'Người dùng', roles: ['Admin'], element: <Users /> },
  { path: '/mycontracts', label: 'Hợp đồng của tôi', roles: ['Tenant'], element: <MyContracts /> },
  { path: '/myinvoices', label: 'Hóa đơn của tôi', roles: ['Tenant'], element: <MyInvoices /> },
  { path: '/myrepairs', label: 'Yêu cầu sửa chữa', roles: ['Tenant'], element: <MyRepairs /> },
]

function Shell() {
  const { user, logout, hasAny, isTenant } = useAuth()
  const loc = useLocation()

  if (!user) return <Login />

  const pages = PAGES.filter(p => hasAny(p.roles))

  return (
    <>
      <header className="topbar">
        <div className="brand">🏠 Quản lý phòng trọ</div>
        <nav className="nav">
          {pages.map(p => (
            <NavLink key={p.path} to={p.path} className={({ isActive }) => (isActive ? 'active' : '')}>
              {p.path === '/home' && isTenant && p.tenantLabel ? p.tenantLabel : p.label}
            </NavLink>
          ))}
        </nav>
        <div className="userbox">
          <NotificationBell />
          <span>{user.fullName || user.username}</span>
          <span className="role">({(user.roles || []).map(roleName).join(' · ')})</span>
          <button className="btn ghost sm" onClick={logout} type="button">Thoát</button>
        </div>
      </header>

      <main className="view fx" key={loc.pathname + user.id}>
        <Routes>
          {pages.map(p => <Route key={p.path} path={p.path} element={p.element} />)}
          <Route path="*" element={<Navigate to="/home" replace />} />
        </Routes>
      </main>
    </>
  )
}

export function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <HashRouter>
          <Shell />
        </HashRouter>
      </ToastProvider>
    </AuthProvider>
  )
}
