import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, store } from './api'
import type { TokenResponse, UserDto } from './types'

interface AuthState {
  user: UserDto | null
  login: (username: string, password: string) => Promise<void>
  logout: () => void
  hasAny: (roles: string[]) => boolean
  isTenant: boolean
  isOwner: boolean
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(store.user)

  useEffect(() => {
    const onExpired = () => setUser(null)
    window.addEventListener('auth:expired', onExpired)
    return () => window.removeEventListener('auth:expired', onExpired)
  }, [])

  const login = async (username: string, password: string) => {
    const d = await api.post<TokenResponse>('/auth/login', { username, password })
    store.save(d)
    setUser(d.user)
  }
  const logout = () => { store.clear(); setUser(null) }

  const hasAny = (roles: string[]) => (user?.roles || []).some(r => roles.includes(r))

  const value: AuthState = {
    user, login, logout, hasAny,
    isTenant: hasAny(['Tenant']),
    isOwner: hasAny(['Admin', 'Owner']),
  }
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthState {
  const c = useContext(AuthContext)
  if (!c) throw new Error('useAuth phải dùng trong AuthProvider')
  return c
}
