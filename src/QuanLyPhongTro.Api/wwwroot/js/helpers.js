/* ---------- helpers: lưu trữ, gọi API, tiện ích UI ---------- */
const API_BASE = '/api';

const store = {
  get token() { return localStorage.getItem('qpt_at'); },
  get refresh() { return localStorage.getItem('qpt_rt'); },
  get user() { try { return JSON.parse(localStorage.getItem('qpt_user')); } catch { return null; } },
  has: () => !!localStorage.getItem('qpt_at'),
  save: (data) => {
    localStorage.setItem('qpt_at', data.accessToken);
    localStorage.setItem('qpt_rt', data.refreshToken);
    localStorage.setItem('qpt_user', JSON.stringify(data.user));
  },
  clear: () => { ['qpt_at', 'qpt_rt', 'qpt_user'].forEach(k => localStorage.removeItem(k)); }
};

async function request(path, method = 'GET', body) {
  const headers = {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const t = store.token;
  if (t) headers['Authorization'] = 'Bearer ' + t;
  let res;
  try {
    res = await fetch(API_BASE + path, { method, headers, body: body !== undefined ? JSON.stringify(body) : undefined });
  } catch (e) {
    throw new Error('Không kết nối được máy chủ.');
  }
  if (res.status === 401) {
    store.clear();
    window.location.hash = '#/login';
    throw new Error('Hết phiên đăng nhập, vui lòng đăng nhập lại.');
  }
  const text = await res.text();
  let data = null;
  if (text) { try { data = JSON.parse(text); } catch { /* noop */ } }
  if (!res.ok) {
    const msg = (data && (data.message || data.title)) || ('Lỗi ' + res.status);
    throw new Error(msg);
  }
  return data; // 204 -> null
}

const api = {
  get: p => request(p),
  post: (p, b) => request(p, 'POST', b ?? {}),
  put: (p, b) => request(p, 'PUT', b ?? {}),
  del: p => request(p, 'DELETE')
};

async function apiDownload(path, filename) {
  const headers = {};
  const t = store.token; if (t) headers['Authorization'] = 'Bearer ' + t;
  const res = await fetch(API_BASE + path, { headers });
  if (!res.ok) throw new Error('Xuất file thất bại.');
  const blob = await res.blob();
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = filename;
  document.body.appendChild(a); a.click(); a.remove();
  setTimeout(() => URL.revokeObjectURL(a.href), 4000);
}

/* ---------- định dạng ---------- */
function vnd(x) { const n = Number(x) || 0; return n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫'; }
function dnum(x) { const n = Number(x) || 0; return n.toLocaleString('vi-VN', { maximumFractionDigits: 2 }); }
function fmtDate(iso) {
  if (!iso) return '—';
  const d = new Date(iso);
  return isNaN(d) ? String(iso) : d.toLocaleDateString('vi-VN');
}
function monthInputVal(v) { if (!v) return ''; const m = String(v).match(/^(\d{4})-(\d{2})/); return m ? m[1] + '-' + m[2] : v; }
function esc(s) { return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }
function val(id) { const el = document.getElementById(id); return el ? el.value.trim() : ''; }
function num(id, dflt) { const v = parseFloat(document.getElementById(id)?.value); return isNaN(v) ? (dflt ?? 0) : v; }

/* ---------- UI ---------- */
function toast(msg, type = 'ok') {
  const root = document.getElementById('toast-root');
  const d = document.createElement('div');
  d.className = 'toast ' + type;
  d.textContent = msg;
  root.appendChild(d);
  setTimeout(() => d.remove(), 3200);
}

function badge(text, kind) { return '<span class="badge ' + (kind || '') + '">' + esc(text) + '</span>'; }

function statusBadge(statusName) {
  const s = String(statusName || '');
  let kind = '';
  if (/thanh toán|đã|hoàn|cho thuê|hiệu lực|tiếp nhận|đang xử|trống/.test(s)) kind = 'ok';
  if (/chờ|một phần/.test(s)) kind = 'warn';
  if (/hủy|chấm dứt|hết hạn|từ chối|bảo trì/.test(s)) kind = 'bad';
  if (/chưa thanh toán/.test(s)) kind = 'bad';
  return badge(s, kind);
}

function openModal(title, bodyHtml) {
  const root = document.getElementById('modal-root');
  root.innerHTML = '<div class="modal-backdrop"><div class="modal"><button class="x" onclick="closeModal()">✕</button><h3>' + esc(title) + '</h3><div id="modal-body">' + bodyHtml + '</div></div></div>';
  root.querySelector('.modal-backdrop').addEventListener('click', e => { if (e.target === e.currentTarget) closeModal(); });
  return root.querySelector('#modal-body');
}
function closeModal() { document.getElementById('modal-root').innerHTML = ''; }
function modalButtons(actionsHtml) {
  const wrap = document.createElement('div');
  wrap.className = 'form-actions';
  wrap.innerHTML = '<button class="btn gray" onclick="closeModal()">Đóng</button>' + (actionsHtml || '');
  return wrap;
}
