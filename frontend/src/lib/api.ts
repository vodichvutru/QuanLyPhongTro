import type { TokenResponse, UserDto } from './types'

const API_BASE = '/api'

export const store = {
  get token() { return localStorage.getItem('qpt_at') },
  get refresh() { return localStorage.getItem('qpt_rt') },
  get user(): UserDto | null {
    try { return JSON.parse(localStorage.getItem('qpt_user') || 'null') } catch { return null }
  },
  has: () => !!localStorage.getItem('qpt_at'),
  save: (d: TokenResponse) => {
    localStorage.setItem('qpt_at', d.accessToken)
    localStorage.setItem('qpt_rt', d.refreshToken)
    localStorage.setItem('qpt_user', JSON.stringify(d.user))
  },
  clear: () => { ['qpt_at', 'qpt_rt', 'qpt_user'].forEach(k => localStorage.removeItem(k)) },
}

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) { super(message); this.status = status }
}

export async function request<T = unknown>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  const t = store.token
  if (t) headers['Authorization'] = 'Bearer ' + t

  let res: Response
  try {
    res = await fetch(API_BASE + path, {
      method, headers, body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ApiError('Không kết nối được máy chủ.', 0)
  }

  if (res.status === 401) {
    store.clear()
    window.dispatchEvent(new Event('auth:expired'))
    throw new ApiError('Hết phiên đăng nhập, vui lòng đăng nhập lại.', 401)
  }

  const text = await res.text()
  let data: unknown = null
  if (text) { try { data = JSON.parse(text) } catch { /* noop */ } }
  if (!res.ok) {
    const d = data as { message?: string; title?: string } | null
    throw new ApiError((d && (d.message || d.title)) || ('Lỗi ' + res.status), res.status)
  }
  return data as T
}

export const api = {
  get: <T = unknown>(p: string) => request<T>(p),
  post: <T = unknown>(p: string, b?: unknown) => request<T>(p, 'POST', b ?? {}),
  put: <T = unknown>(p: string, b?: unknown) => request<T>(p, 'PUT', b ?? {}),
  del: <T = unknown>(p: string) => request<T>(p, 'DELETE'),
}

export async function apiDownload(path: string, filename: string) {
  const headers: Record<string, string> = {}
  const t = store.token; if (t) headers['Authorization'] = 'Bearer ' + t
  const res = await fetch(API_BASE + path, { headers })
  if (!res.ok) throw new ApiError('Xuất file thất bại.', res.status)
  const blob = await res.blob()
  const a = document.createElement('a')
  a.href = URL.createObjectURL(blob)
  a.download = filename
  document.body.appendChild(a); a.click(); a.remove()
  setTimeout(() => URL.revokeObjectURL(a.href), 4000)
}
