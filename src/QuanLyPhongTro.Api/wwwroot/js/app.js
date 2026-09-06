/* ================= Quản lý Phòng Trọ — SPA ================= */
/* ---------- trạng thái & điều hướng ---------- */
function hasAny(list) { const r = (store.user?.roles) || []; return r.some(x => list.includes(x)); }
function isTenant() { return hasAny(['Tenant']); }
function isOwner() { return hasAny(['Admin', 'Owner']); }

const R = {
  roles: ['Admin', 'Owner', 'Tenant']
};
const PAGES = {
  home:    { label: isTenant() ? 'Trang chủ' : 'Dashboard', roles: ['Admin', 'Owner', 'Tenant'], view: vHome },
  rooms:   { label: 'Phòng trọ', roles: ['Admin', 'Owner'], view: vRooms },
  tenants: { label: 'Người thuê', roles: ['Admin', 'Owner'], view: vTenants },
  contracts:{ label: 'Hợp đồng', roles: ['Admin', 'Owner'], view: vContracts },
  meter:   { label: 'Chỉ số điện nước', roles: ['Admin', 'Owner'], view: vMeter },
  invoices:{ label: 'Hóa đơn', roles: ['Admin', 'Owner'], view: vInvoices },
  repairs: { label: 'Sửa chữa', roles: ['Admin', 'Owner'], view: vRepairs },
  reports: { label: 'Báo cáo', roles: ['Admin', 'Owner'], view: vReports },
  users:   { label: 'Người dùng', roles: ['Admin'], view: vUsers },
  mycontracts:{ label: 'Hợp đồng của tôi', roles: ['Tenant'], view: vMyContracts },
  myinvoices:{ label: 'Hóa đơn của tôi', roles: ['Tenant'], view: vMyInvoices },
  myrepairs:{ label: 'Yêu cầu sửa chữa', roles: ['Tenant'], view: vMyRepairs }
};

function navList() {
  const out = [];
  for (const [key, p] of Object.entries(PAGES)) {
    if (!hasAny(p.roles)) continue;
    let label = p.label;
    if (key === 'home') label = isTenant() ? 'Trang chủ' : 'Dashboard';
    out.push({ key, label });
  }
  return out;
}

async function logout() { store.clear(); location.hash = '#/login'; }

function pageKey() { return (location.hash || '#/home').replace(/^#\//, '').split('?')[0] || 'home'; }

async function route() {
  const view = document.getElementById('view');
  const top = document.getElementById('topbar');
  if (!store.has()) {
    top.classList.add('hidden');
    view.innerHTML = renderLogin();
    bindLogin();
    return;
  }
  top.classList.remove('hidden');
  renderNav();
  const key = pageKey();
  const page = PAGES[key];
  if (!page || !hasAny(page.roles)) { location.hash = '#/home'; return; }
  view.innerHTML = '<div class="center muted">Đang tải…</div>';
  try { await page.view(view); } catch (e) { view.innerHTML = '<div class="card"><b>Lỗi:</b> ' + esc(e.message) + '</div>'; }
}

function renderNav() {
  const nav = document.getElementById('nav');
  const key = pageKey();
  nav.innerHTML = navList().map(n =>
    '<button data-r="#/' + n.key + '" class="' + (n.key === key ? 'active' : '') + '">' + esc(n.label) + '</button>').join('');
  nav.querySelectorAll('button').forEach(b => b.onclick = () => { location.hash = b.dataset.r; });

  const u = store.user || {};
  const box = document.getElementById('userbox');
  box.innerHTML = '<span>' + esc(u.fullName || u.username || '') + '</span>' +
    '<span class="role">(' + esc((u.roles || []).join(', ')) + ')</span>' +
    '<button class="btn ghost sm" onclick="logout()">Thoát</button>';
}

/* ---------- Đăng nhập ---------- */
function renderLogin() {
  return '<div class="login-wrap"><div class="login-card">' +
    '<h1>🏠 Quản lý phòng trọ</h1>' +
    '<div class="sub">Đăng nhập để tiếp tục</div>' +
    '<label>Tên đăng nhập</label><input id="lg_user" value="chutro" autocomplete="username">' +
    '<label>Mật khẩu</label><input id="lg_pass" type="password" value="123456" autocomplete="current-password">' +
    '<div style="margin-top:18px"><button class="btn" id="lg_btn" style="width:100%;justify-content:center">Đăng nhập</button></div>' +
    '<div class="demo-hint"><b>Tài khoản demo:</b> chutro/123456 (Chủ trọ) · admin/123456 (Quản trị) · tenant1/123456 (Người thuê)</div>' +
    '</div></div>';
}
function bindLogin() {
  const doLogin = async () => {
    const u = val('lg_user'), p = val('lg_pass');
    if (!u || !p) return toast('Nhập tên đăng nhập và mật khẩu.', 'err');
    try {
      const d = await api.post('/auth/login', { username: u, password: p });
      store.save(d);
      toast('Đăng nhập thành công!');
      location.hash = '#/home';
    } catch (e) { toast(e.message, 'err'); }
  };
  document.getElementById('lg_btn').onclick = doLogin;
  document.getElementById('lg_pass').addEventListener('keydown', e => { if (e.key === 'Enter') doLogin(); });
  document.getElementById('lg_user').focus();
}

/* ---------- tiện ích chung màn hình ---------- */
function pageHead(title, actionsHtml) {
  return '<div class="page-head"><h1>' + esc(title) + '</h1><div>' + (actionsHtml || '') + '</div></div>';
}
function kpiCards(cards) {
  return '<div class="kpis">' + cards.map(c =>
    '<div class="kpi"><div class="t">' + esc(c.t) + '</div><div class="v">' + c.v + '</div>' +
    (c.s ? '<div class="s">' + c.s + '</div>' : '') + '</div>').join('') + '</div>';
}
function emptyRow(cols) { return '<tr><td colspan="' + cols + '" class="empty">Chưa có dữ liệu</td></tr>'; }
function selOpts(opts, selected, labelKey, valKey) {
  if (!opts || !opts.length) return '';
  return opts.map(o => {
    const v = valKey ? o[valKey] : o.id;
    const l = typeof labelKey === 'function' ? labelKey(o) : (labelKey ? o[labelKey] : o);
    return '<option value="' + esc(v) + '"' + (String(v) === String(selected) ? ' selected' : '') + '>' + esc(l) + '</option>';
  }).join('');
}

/* ================= DASHBOARD ================= */
async function vHome(view) {
  if (isTenant()) return vTenantHome(view);
  const [dash, debt] = await Promise.all([api.get('/reports/dashboard'), api.get('/reports/debt')]);
  const k = [
    { t: 'Tổng phòng', v: dash.totalRooms, s: dash.rentedRooms + ' đang cho thuê' },
    { t: 'Đang cho thuê', v: dash.rentedRooms, s: 'Trống: ' + dash.availableRooms },
    { t: 'Người thuê', v: dash.totalTenants, s: 'Đang hoạt động: ' + dash.activeTenants },
    { t: 'Doanh thu tháng này', v: vnd(dash.revenueThisMonth) },
    { t: 'Công nợ', v: vnd(dash.totalOutstanding), s: dash.openInvoices + ' hóa đơn chưa trả đủ' }
  ];
  let html = pageHead('Dashboard', '') + kpiCards(k) +
    '<div class="card"><h3 style="margin-top:0">📋 Công nợ chưa thu</h3>' +
    '<table><thead><tr><th>Hóa đơn</th><th>Phòng</th><th>Người thuê</th><th>Kỳ</th><th>Hạn</th><th class="num">Tổng</th><th class="num">Còn lại</th></tr></thead><tbody>';
  html += (debt.length ? debt.map(d =>
    '<tr><td>' + esc(d.invoiceCode) + '</td><td>' + esc(d.roomName) + '</td><td>' + esc(d.tenantName) + '</td>' +
    '<td>' + esc(d.billingMonth) + '</td><td>' + fmtDate(d.dueDate) + '</td>' +
    '<td class="num">' + vnd(d.totalAmount) + '</td><td class="num"><b>' + vnd(d.remaining) + '</b></td></tr>').join('')
    : emptyRow(7)) + '</tbody></table></div>';
  view.innerHTML = html;
}

async function vTenantHome(view) {
  const [inv, cons] = await Promise.all([api.get('/me/invoices'), api.get('/me/contracts')]);
  const open = inv.filter(i => i.status === 'Unpaid' || i.status === 'PartiallyPaid');
  const due = open.reduce((s, i) => s + (i.totalAmount - i.paidAmount), 0);
  const activeC = cons.filter(c => c.status === 'Active');
  const k = [
    { t: 'Hợp đồng đang hiệu lực', v: activeC.length },
    { t: 'Hóa đơn chưa thanh toán', v: open.length, s: 'trên ' + inv.length + ' hóa đơn' },
    { t: 'Số cần thanh toán', v: vnd(due), s: 'tổng các kỳ còn nợ' }
  ];
  let html = pageHead('Xin chào, ' + esc((store.user || {}).fullName || '') + ' 👋', '') + kpiCards(k);
  html += '<div class="card"><h3 style="margin-top:0">🧾 Hóa đơn cần thanh toán</h3><table><thead><tr><th>Mã</th><th>Phòng</th><th>Kỳ</th><th class="num">Tổng</th><th class="num">Đã trả</th><th>Trạng thái</th></tr></thead><tbody>';
  html += (open.length ? open.map(i =>
    '<tr><td>' + esc(i.invoiceCode) + '</td><td>' + esc(i.roomName) + '</td><td>' + esc(i.billingMonth) + '</td>' +
    '<td class="num">' + vnd(i.totalAmount) + '</td><td class="num">' + vnd(i.paidAmount) + '</td><td>' + statusBadge(i.statusName) + '</td></tr>').join('')
    : emptyRow(6)) + '</tbody></table>' +
    '<div class="muted small" style="margin-top:8px">Xem chi tiết tại mục <b>Hóa đơn của tôi</b>.</div></div>';
  view.innerHTML = html;
}

/* ================= ROOMS ================= */
async function vRooms(view) {
  const rooms = await api.get('/rooms');
  view.innerHTML = pageHead('Phòng trọ', '<button class="btn" onclick="roomForm()">+ Thêm phòng</button>') +
    '<div class="card"><table><thead><tr><th>Phòng</th><th>Tầng</th><th class="num">Diện tích</th><th class="num">Giá/tháng</th><th>SL tối đa</th><th>Trạng thái</th><th style="width:120px"></th></tr></thead><tbody>' +
    (rooms.length ? rooms.map(r =>
      '<tr><td><b>' + esc(r.name) + '</b></td><td>' + esc(r.floor || '—') + '</td><td class="num">' + r.area + ' m²</td>' +
      '<td class="num">' + vnd(r.price) + '</td><td>' + r.maxPeople + '</td><td>' + statusBadge(r.statusName) + '</td>' +
      '<td><button class="btn gray sm" onclick="roomForm(' + r.id + ')">Sửa</button> ' +
      '<button class="btn danger sm" onclick="roomDel(' + r.id + ')">Xóa</button></td></tr>').join('') : emptyRow(7)) +
    '</tbody></table></div>';
}

async function roomForm(id) {
  const rooms = await api.get('/rooms');
  const r = id ? rooms.find(x => x.id === id) : null;
  const b = openModal(r ? 'Sửa phòng ' + r.name : 'Thêm phòng mới',
    '<div class="form-grid">' +
    '<div><label>Mã phòng</label><input id="r_name" value="' + esc(r?.name || '') + '"></div>' +
    '<div><label>Tầng</label><input id="r_floor" value="' + esc(r?.floor || '') + '"></div>' +
    '<div><label>Diện tích (m²)</label><input id="r_area" type="number" value="' + (r?.area ?? '') + '"></div>' +
    '<div><label>Giá phòng (₫/tháng)</label><input id="r_price" type="number" value="' + (r?.price ?? '') + '"></div>' +
    '<div><label>SL người tối đa</label><input id="r_max" type="number" value="' + (r?.maxPeople ?? 2) + '"></div>' +
    (r ? '<div><label>Trạng thái</label><select id="r_status">' +
      ['Available', 'Rented', 'Maintenance'].map(s => '<option value="' + s + '"' + (r.status === s ? ' selected' : '') + '>' + s + '</option>').join('') + '</select></div>' : '') +
    '<div style="grid-column:1/-1"><label>Ghi chú</label><input id="r_note" value="' + esc(r?.note || '') + '"></div>' +
    '</div>');
  b.appendChild(modalButtons('<button class="btn" onclick="roomSave(' + (id ?? 'null') + ')">Lưu</button>'));
}
async function roomSave(id) {
  const body = {
    name: val('r_name'), floor: val('r_floor') || null, area: num('r_area'), price: num('r_price'),
    maxPeople: num('r_max', 2), note: val('r_note') || null
  };
  if (id) body.status = val('r_status');
  try {
    if (id) await api.put('/rooms/' + id, body); else await api.post('/rooms', body);
    closeModal(); toast('Đã lưu phòng'); route();
  } catch (e) { toast(e.message, 'err'); }
}
async function roomDel(id) {
  if (!confirm('Xóa phòng này?')) return;
  try { await api.del('/rooms/' + id); toast('Đã xóa'); route(); }
  catch (e) { toast(e.message, 'err'); }
}

/* ================= TENANTS ================= */
async function vTenants(view) {
  const list = await api.get('/tenants');
  view.innerHTML = pageHead('Người thuê', '<button class="btn" onclick="tenantForm()">+ Thêm người thuê</button>') +
    '<div class="card"><table><thead><tr><th>Họ tên</th><th>SĐT</th><th>CCCD</th><th>Email</th><th>Trạng thái</th><th style="width:120px"></th></tr></thead><tbody>' +
    (list.length ? list.map(t =>
      '<tr><td><b>' + esc(t.fullName) + '</b>' + (t.hasAccount ? ' ' + badge('có tài khoản', 'brand') : '') + '</td>' +
      '<td>' + esc(t.phone || '—') + '</td><td>' + esc(t.identityNumber || '—') + '</td><td>' + esc(t.email || '—') + '</td>' +
      '<td>' + (t.isActive ? badge('Hoạt động', 'ok') : badge('Ngưng', 'bad')) + '</td>' +
      '<td><button class="btn gray sm" onclick="tenantForm(' + t.id + ')">Sửa</button> ' +
      '<button class="btn danger sm" onclick="tenantDel(' + t.id + ')">Xóa</button></td></tr>').join('') : emptyRow(6)) +
    '</tbody></table></div>';
}
async function tenantForm(id) {
  const list = await api.get('/tenants');
  const t = id ? list.find(x => x.id === id) : null;
  const b = openModal(t ? 'Sửa người thuê' : 'Thêm người thuê',
    '<div class="form-grid">' +
    '<div style="grid-column:1/-1"><label>Họ tên *</label><input id="t_name" value="' + esc(t?.fullName || '') + '"></div>' +
    '<div><label>SĐT</label><input id="t_phone" value="' + esc(t?.phone || '') + '"></div>' +
    '<div><label>CCCD/CMND</label><input id="t_idn" value="' + esc(t?.identityNumber || '') + '"></div>' +
    '<div><label>Email</label><input id="t_email" value="' + esc(t?.email || '') + '"></div>' +
    '<div><label>Địa chỉ</label><input id="t_addr" value="' + esc(t?.address || '') + '"></div>' +
    '<div style="grid-column:1/-1"><label>Ghi chú</label><input id="t_note" value="' + esc(t?.note || '') + '"></div>' +
    (t ? '<div><label>Hoạt động</label><select id="t_active"><option value="true"' + (t.isActive ? ' selected' : '') + '>Hoạt động</option><option value="false"' + (!t.isActive ? ' selected' : '') + '>Ngưng</option></select></div>' : '') +
    '</div>');
  b.appendChild(modalButtons('<button class="btn" onclick="tenantSave(' + (id ?? 'null') + ')">Lưu</button>'));
}
async function tenantSave(id) {
  const body = {
    fullName: val('t_name'), phone: val('t_phone') || null, identityNumber: val('t_idn') || null,
    email: val('t_email') || null, address: val('t_addr') || null, note: val('t_note') || null
  };
  if (id) body.isActive = val('t_active') === 'true';
  try {
    if (id) await api.put('/tenants/' + id, body); else await api.post('/tenants', body);
    closeModal(); toast('Đã lưu'); route();
  } catch (e) { toast(e.message, 'err'); }
}
async function tenantDel(id) {
  if (!confirm('Xóa người thuê này?')) return;
  try { await api.del('/tenants/' + id); toast('Đã xóa'); route(); }
  catch (e) { toast(e.message, 'err'); }
}

/* ================= CONTRACTS ================= */
async function vContracts(view) {
  const list = await api.get('/contracts');
  view.innerHTML = pageHead('Hợp đồng thuê', '<button class="btn" onclick="contractForm()">+ Lập hợp đồng</button>') +
    '<div class="card"><table><thead><tr><th>Mã HĐ</th><th>Phòng</th><th>Người thuê</th><th>Thời hạn</th><th class="num">Tiền phòng</th><th>Trạng thái</th><th style="width:170px"></th></tr></thead><tbody>' +
    (list.length ? list.map(c =>
      '<tr><td>' + esc(c.contractCode) + '</td><td><b>' + esc(c.roomName) + '</b></td><td>' + esc(c.tenantName) + '</td>' +
      '<td class="small">' + fmtDate(c.startDate) + ' → ' + fmtDate(c.endDate) + '</td>' +
      '<td class="num">' + vnd(c.monthlyRent) + '</td><td>' + statusBadge(c.statusName) + '</td>' +
      '<td><button class="btn gray sm" onclick="contractView(' + c.id + ')">Xem</button> ' +
      (c.status === 'Active' ? '<button class="btn danger sm" onclick="contractTerm(' + c.id + ')">Chấm dứt</button>' : '') + '</td></tr>').join('') : emptyRow(7)) +
    '</tbody></table></div>';
}
async function contractView(id) {
  const c = await api.get('/contracts/' + id);
  const b = openModal('Hợp đồng ' + c.contractCode,
    rowsOf([
      ['Phòng', c.roomName], ['Người thuê', c.tenantName],
      ['Thời hạn', fmtDate(c.startDate) + ' → ' + fmtDate(c.endDate)],
      ['Tiền phòng / tháng', vnd(c.monthlyRent)], ['Tiền đặt cọc', vnd(c.deposit)],
      ['Giá điện', vnd(c.electricPrice) + ' / kWh'], ['Giá nước', vnd(c.waterPrice) + ' / m³'],
      ['Trạng thái', statusBadge(c.statusName)], ['Ghi chú', c.note || '—']
    ]));
  b.appendChild(modalButtons(''));
}
async function contractForm() {
  const [rooms, tenants] = await Promise.all([api.get('/rooms'), api.get('/tenants')]);
  const free = rooms.filter(r => r.status === 'Available');
  const b = openModal('Lập hợp đồng mới',
    '<div class="form-grid">' +
    '<div><label>Phòng</label><select id="c_room"><option value="">— Chọn phòng trống —</option>' + selOpts(free, null, o => o.name + ' (' + vnd(o.price) + '/tháng)') + '</select></div>' +
    '<div><label>Người thuê</label><select id="c_ten"><option value="">— Chọn người thuê —</option>' + selOpts(tenants, null, 'fullName') + '</select></div>' +
    '<div><label>Ngày bắt đầu</label><input id="c_start" type="date" value="' + todayStr() + '"></div>' +
    '<div><label>Ngày kết thúc (bỏ trống = mở)</label><input id="c_end" type="date"></div>' +
    '<div><label>Tiền phòng (₫/tháng)</label><input id="c_rent" type="number" value="2000000"></div>' +
    '<div><label>Tiền đặt cọc</label><input id="c_dep" type="number" value="0"></div>' +
    '<div><label>Giá điện (₫/kWh)</label><input id="c_elec" type="number" value="3500"></div>' +
    '<div><label>Giá nước (₫/m³)</label><input id="c_water" type="number" value="25000"></div>' +
    '</div>');
  b.appendChild(modalButtons('<button class="btn" onclick="contractSave()">Lưu hợp đồng</button>'));
}
async function contractSave() {
  const roomId = num('c_room'), tenantId = num('c_ten');
  if (!roomId || !tenantId) return toast('Chọn phòng và người thuê.', 'err');
  const body = {
    roomId, tenantId,
    startDate: val('c_start'), endDate: val('c_end') || null,
    monthlyRent: num('c_rent'), deposit: num('c_dep'), electricPrice: num('c_elec'), waterPrice: num('c_water')
  };
  try { await api.post('/contracts', body); closeModal(); toast('Đã lập hợp đồng'); route(); }
  catch (e) { toast(e.message, 'err'); }
}
async function contractTerm(id) {
  const date = prompt('Ngày chấm dứt (yyyy-mm-dd hoặc để trống = hôm nay):') || '';
  if (date === null) return;
  try {
    await api.post('/contracts/' + id + '/terminate', { terminatedAt: date || null });
    toast('Đã chấm dứt hợp đồng'); route();
  } catch (e) { toast(e.message, 'err'); }
}

/* ================= METER ================= */
let meterRooms = [];
async function vMeter(view) {
  meterRooms = await api.get('/rooms');
  view.innerHTML = pageHead('Ghi chỉ số điện nước', '') +
    '<div class="filters"><label style="margin:0">Phòng</label><select id="m_room" onchange="meterLoad()">' + selOpts(meterRooms, null, o => o.name) + '</select>' +
    '<button class="btn" onclick="meterForm()">+ Ghi chỉ số</button></div>' +
    '<div id="m_table" class="card"><div class="empty">Chọn phòng…</div></div>';
  meterLoad();
}
async function meterLoad() {
  const roomId = num('m_room');
  if (!roomId) return;
  const list = await api.get('/meter-readings/room/' + roomId);
  document.getElementById('m_table').innerHTML =
    '<table><thead><tr><th>Ngày ghi</th><th class="num">Điện (kWh)</th><th class="num">Nước (m³)</th><th>Ghi chú</th></tr></thead><tbody>' +
    (list.length ? list.map(m =>
      '<tr><td>' + fmtDate(m.readingDate) + '</td><td class="num">' + m.electricIndex + '</td><td class="num">' + m.waterIndex + '</td><td>' + esc(m.note || '') + '</td></tr>').join('') : emptyRow(4)) +
    '</tbody></table>';
}
function meterForm() {
  const b = openModal('Ghi chỉ số mới',
    '<div class="form-grid">' +
    '<div><label>Ngày ghi</label><input id="m_date" type="date" value="' + todayStr() + '"></div>' +
    '<div><label>Điện (kWh)</label><input id="m_e" type="number" value="0" step="any"></div>' +
    '<div><label>Nước (m³)</label><input id="m_w" type="number" value="0" step="any"></div>' +
    '</div>');
  b.appendChild(modalButtons('<button class="btn" onclick="meterSave()">Lưu</button>'));
}
async function meterSave() {
  const body = { roomId: num('m_room'), readingDate: val('m_date'), electricIndex: num('m_e'), waterIndex: num('m_w') };
  try { await api.post('/meter-readings', body); closeModal(); toast('Đã ghi chỉ số'); meterLoad(); }
  catch (e) { toast(e.message, 'err'); }
}

/* ================= INVOICES ================= */
async function vInvoices(view) {
  const month = monthInputVal(val('f_inv_month')) || monthNow();
  view.innerHTML = pageHead('Hóa đơn tiền thuê', '<button class="btn" onclick="invoiceForm()">+ Lập hóa đơn</button>') +
    '<div class="filters"><label style="margin:0">Kỳ</label><input id="f_inv_month" type="month" value="' + month + '" onchange="invFilter()">' +
    '<select id="f_inv_status" onchange="invFilter()"><option value="">Tất cả trạng thái</option>' +
    ['Unpaid', 'PartiallyPaid', 'Paid', 'Cancelled'].map(s => '<option value="' + s + '">' + s + '</option>').join('') + '</select></div>' +
    '<div id="inv_table"></div>';
  invFilter();
}
async function invFilter() {
  const month = val('f_inv_month'); const status = val('f_inv_status');
  const qs = new URLSearchParams();
  if (month) qs.set('month', month);
  if (status) qs.set('status', status);
  const list = await api.get('/invoices?' + qs.toString());
  document.getElementById('inv_table').innerHTML =
    '<div class="card"><table><thead><tr><th>Mã</th><th>Kỳ</th><th>Phòng</th><th>Người thuê</th><th class="num">Tổng</th><th class="num">Đã trả</th><th>Trạng thái</th><th style="width:150px"></th></tr></thead><tbody>' +
    (list.length ? list.map(i =>
      '<tr><td>' + esc(i.invoiceCode) + '</td><td>' + esc(i.billingMonth) + '</td><td><b>' + esc(i.roomName) + '</b></td><td>' + esc(i.tenantName) + '</td>' +
      '<td class="num">' + vnd(i.totalAmount) + '</td><td class="num">' + vnd(i.paidAmount) + '</td><td>' + statusBadge(i.statusName) + '</td>' +
      '<td><button class="btn gray sm" onclick="invoiceView(' + i.id + ')">Xem & thu</button> ' +
      (i.status !== 'Cancelled' ? '<button class="btn danger sm" onclick="invoiceCancel(' + i.id + ')">Hủy</button>' : '') + '</td></tr>').join('') : emptyRow(8)) +
    '</tbody></table></div>';
}
async function invoiceForm() {
  const rooms = await api.get('/rooms');
  const b = openModal('Lập hóa đơn theo kỳ (tự tính điện/nước)',
    '<div class="small muted" style="margin-top:6px">Chương trình tự tìm hợp đồng còn hiệu lực, lấy chỉ số đồng hồ đầu/cuối kỳ và cộng tiền phòng.</div>' +
    '<div class="form-grid">' +
    '<div><label>Phòng</label><select id="i_room">' + selOpts(rooms, null, o => o.name) + '</select></div>' +
    '<div><label>Kỳ thanh toán</label><input id="i_month" type="month" value="' + monthNow() + '"></div>' +
    '<div><label>Hạn thanh toán</label><input id="i_due" type="date"></div>' +
    '</div>' +
    '<label>Khoản phí thêm (không bắt buộc)</label><div id="extraRows"></div>' +
    '<button class="btn gray sm" onclick="addExtra()" style="margin-top:6px">+ Thêm khoản phí</button>');
  addExtra();
  b.appendChild(modalButtons('<button class="btn" onclick="invoiceSave()">Lập hóa đơn</button>'));
}
function addExtra() {
  const rows = document.getElementById('extraRows');
  if (!rows) return;
  const d = document.createElement('div');
  d.style.cssText = 'display:flex;gap:8px;margin-top:6px';
  d.innerHTML = '<input class="ex-name" placeholder="Tên phí (VD: Phí vệ sinh)" style="flex:2">' +
    '<input class="ex-amount" type="number" placeholder="Số tiền" style="flex:1">' +
    '<button class="btn gray sm" onclick="this.parentNode.remove()">✕</button>';
  rows.appendChild(d);
}
async function invoiceSave() {
  const extras = [];
  document.querySelectorAll('#extraRows > div').forEach(d => {
    const n = d.querySelector('.ex-name').value.trim(), a = parseFloat(d.querySelector('.ex-amount').value);
    if (n && !isNaN(a)) extras.push({ name: n, amount: a });
  });
  const body = { roomId: num('i_room'), billingMonth: val('i_month'), dueDate: val('i_due') || null, extraItems: extras.length ? extras : null };
  try { await api.post('/invoices', body); closeModal(); toast('Đã lập hóa đơn'); route(); }
  catch (e) { toast(e.message, 'err'); }
}
async function invoiceView(id) {
  const i = await api.get('/invoices/' + id);
  const rem = i.totalAmount - i.paidAmount;
  const b = openModal('Hóa đơn ' + i.invoiceCode,
    rowsOf([
      ['Phòng / Người thuê', i.roomName + ' — ' + i.tenantName], ['Kỳ', i.billingMonth],
      ['Hạn thanh toán', fmtDate(i.dueDate)], ['Nợ kỳ trước', vnd(i.previousDebt)],
      ['Trạng thái', statusBadge(i.statusName)]
    ]) +
    '<h4>Chi tiết</h4><table><thead><tr><th>Khoản</th><th>SL</th><th>Đơn giá</th><th class="num">Thành tiền</th></tr></thead><tbody>' +
    (i.items || []).map(x => '<tr><td>' + esc(x.name) + '</td><td>' + (x.unit ? x.quantity + ' ' + esc(x.unit) : '') + '</td><td>' + vnd(x.unitPrice) + '</td><td class="num">' + vnd(x.amount) + '</td></tr>').join('') +
    '</tbody></table><div style="text-align:right;font-weight:700;font-size:16px;margin:8px 0">Tổng: ' + vnd(i.totalAmount) + '</div>' +
    '<h4>Lịch sử thanh toán</h4>' + (i.payments && i.payments.length ?
      '<table><thead><tr><th>Ngày</th><th class="num">Số tiền</th><th>Phương thức</th></tr></thead><tbody>' +
      i.payments.map(p => '<tr><td>' + fmtDate(p.paidAt) + '</td><td class="num">' + vnd(p.amount) + '</td><td>' + esc(p.methodName) + '</td></tr>').join('') + '</tbody></table>'
      : '<div class="empty" style="padding:10px">Chưa có thanh toán</div>') +
    (rem > 0 ? '<div id="payBlock" style="margin-top:14px;border-top:1px solid var(--line);padding-top:10px">' +
      '<div class="detail-row"><span>Còn phải thu</span><b>' + vnd(rem) + '</b></div>' +
      '<div class="form-grid">' +
      '<div><label>Số tiền</label><input id="pay_amount" type="number" value="' + rem + '"></div>' +
      '<div><label>Phương thức</label><select id="pay_method">' +
      ['Cash', 'BankTransfer', 'Momo', 'VnPay', 'Other'].map(m => '<option value="' + m + '">' + methodName(m) + '</option>').join('') + '</select></div>' +
      '<div style="grid-column:1/-1"><label>Ghi chú</label><input id="pay_note"></div></div>' +
      '<div class="form-actions"><button class="btn ok" onclick="paySave(' + i.id + ')">Ghi nhận thu tiền</button></div></div>'
      : '<div class="muted small" style="margin-top:8px">✅ Đã thanh toán đủ.</div>'));
}
function methodName(m) {
  return { Cash: 'Tiền mặt', BankTransfer: 'Chuyển khoản', Momo: 'Momo', VnPay: 'VNPay', Other: 'Khác' }[m] || m;
}
async function paySave(id) {
  const amount = num('pay_amount');
  if (!(amount > 0)) return toast('Nhập số tiền hợp lệ.', 'err');
  try {
    await api.post('/payments', { invoiceId: id, amount, method: val('pay_method'), note: val('pay_note') || null });
    closeModal(); toast('Đã ghi nhận thanh toán'); route();
  } catch (e) { toast(e.message, 'err'); }
}
async function invoiceCancel(id) {
  if (!confirm('Hủy hóa đơn này?')) return;
  try { await api.post('/invoices/' + id + '/cancel'); toast('Đã hủy'); route(); }
  catch (e) { toast(e.message, 'err'); }
}

/* ================= REPAIRS (owner) ================= */
async function vRepairs(view) {
  view.innerHTML = pageHead('Yêu cầu sửa chữa', '<button class="btn" onclick="repairForm()">+ Lập yêu cầu</button>') +
    '<div class="filters"><select id="f_rep_status" onchange="repairFilter()"><option value="">Tất cả trạng thái</option>' +
    ['Pending', 'Approved', 'InProgress', 'Completed', 'Rejected', 'Cancelled'].map(s => '<option value="' + s + '">' + repairName(s) + '</option>').join('') + '</select></div>' +
    '<div id="rep_table"></div>';
  repairFilter();
}
function repairName(s) {
  return { Pending: 'Chờ xử lý', Approved: 'Đã tiếp nhận', InProgress: 'Đang xử lý', Completed: 'Đã hoàn thành', Rejected: 'Từ chối', Cancelled: 'Đã hủy' }[s] || s;
}
async function repairFilter() {
  const status = val('f_rep_status');
  const qs = status ? '?status=' + encodeURIComponent(status) : '';
  const list = await api.get('/repair-requests' + qs);
  document.getElementById('rep_table').innerHTML =
    '<div class="card"><table><thead><tr><th>Phòng</th><th>Tiêu đề</th><th>Người thuê</th><th>Người tạo</th><th>Ngày</th><th>Trạng thái</th><th style="width:170px"></th></tr></thead><tbody>' +
    (list.length ? list.map(r =>
      '<tr><td><b>' + esc(r.roomName) + '</b></td><td>' + esc(r.subject) + '</td><td>' + esc(r.tenantName || '—') + '</td><td>' + esc(r.createdByName || '—') + '</td>' +
      '<td class="small">' + fmtDate(r.createdAt) + '</td><td>' + statusBadge(r.statusName) + '</td>' +
      '<td><button class="btn gray sm" onclick="repairDetail(' + r.id + ')">Xem</button> ' +
      (r.status !== 'Completed' && r.status !== 'Cancelled' ? '<button class="btn ok sm" onclick="repairUpdate(' + r.id + ')">Xử lý</button>' : '') + '</td></tr>').join('') : emptyRow(7)) +
    '</tbody></table></div>';
}
async function repairDetail(id) {
  const r = await api.get('/repair-requests/' + id);
  const b = openModal('Yêu cầu #' + r.id,
    rowsOf([
      ['Phòng', r.roomName], ['Người thuê', r.tenantName || '—'], ['Người tạo', r.createdByName || '—'],
      ['Ngày gửi', fmtDate(r.createdAt)], ['Trạng thái', statusBadge(r.statusName)],
      ['Mô tả', r.description || '—'], ['Phản hồi', r.ownerNote || '—'],
      ['Chi phí', r.cost != null ? vnd(r.cost) : '—']
    ]));
  b.appendChild(modalButtons(''));
}
async function repairUpdate(id) {
  const r = await api.get('/repair-requests/' + id);
  const b = openModal('Xử lý: ' + r.subject,
    '<label>Trạng thái</label><select id="rp_status">' +
    ['Approved', 'InProgress', 'Completed', 'Rejected'].map(s => '<option value="' + s + '">' + repairName(s) + '</option>').join('') + '</select>' +
    '<label>Phản hồi / ghi chú</label><textarea id="rp_note" rows="2"></textarea>' +
    '<label>Chi phí (₫) — nếu có</label><input id="rp_cost" type="number">');
  b.appendChild(modalButtons('<button class="btn ok" onclick="repairSave(' + id + ')">Cập nhật</button>'));
}
async function repairSave(id) {
  const cost = parseFloat(val('rp_cost'));
  try {
    await api.put('/repair-requests/' + id + '/status', {
      status: val('rp_status'), ownerNote: val('rp_note') || null, cost: isNaN(cost) ? null : cost
    });
    closeModal(); toast('Đã cập nhật'); repairFilter();
  } catch (e) { toast(e.message, 'err'); }
}
async function repairForm() {
  const rooms = await api.get('/rooms');
  const b = openModal('Lập yêu cầu sửa chữa',
    '<label>Phòng</label><select id="rr_room">' + selOpts(rooms, null, o => o.name) + '</select>' +
    '<label>Tiêu đề *</label><input id="rr_subj">' +
    '<label>Mô tả</label><textarea id="rr_desc" rows="2"></textarea>');
  b.appendChild(modalButtons('<button class="btn" onclick="repairCreate()">Gửi yêu cầu</button>'));
}
async function repairCreate() {
  try {
    await api.post('/repair-requests', { roomId: num('rr_room'), subject: val('rr_subj'), description: val('rr_desc') || null });
    closeModal(); toast('Đã tạo yêu cầu'); repairFilter();
  } catch (e) { toast(e.message, 'err'); }
}

/* ================= REPORTS ================= */
async function vReports(view) {
  const dash = await api.get('/reports/dashboard');
  view.innerHTML = pageHead('Báo cáo', '<button class="btn" onclick="exportCsv()">⬇ Xuất doanh thu CSV</button>') +
    kpiCards([
      { t: 'Tổng phòng', v: dash.totalRooms, s: dash.rentedRooms + ' cho thuê / ' + dash.availableRooms + ' trống' },
      { t: 'Người thuê', v: dash.totalTenants },
      { t: 'Doanh thu tháng này', v: vnd(dash.revenueThisMonth) },
      { t: 'Công nợ', v: vnd(dash.totalOutstanding), s: dash.openInvoices + ' hóa đơn' }
    ]) +
    '<div class="card"><h3 style="margin-top:0">💰 Doanh thu theo tháng</h3>' +
    '<div class="filters"><label style="margin:0">Từ</label><input id="rp_from" type="month" value="' + monthShift(-5) + '">' +
    '<label style="margin:0">Đến</label><input id="rp_to" type="month" value="' + monthNow() + '">' +
    '<button class="btn" onclick="revLoad()">Xem</button></div><div id="rev_chart"></div><div id="rev_table"></div></div>' +
    '<div class="card"><h3 style="margin-top:0">📋 Công nợ chi tiết</h3><div id="debt_table"></div></div>' +
    '<div class="card"><h3 style="margin-top:0">📋 Công nợ theo phòng</h3><div id="debtRoom_table"></div></div>';
  revLoad();
  debtLoad();
  debtRoomLoad();
}
async function debtRoomLoad() {
  const d = await api.get('/reports/debt-by-room');
  const el = document.getElementById('debtRoom_table');
  if (!el) return;
  el.innerHTML =
    '<table><thead><tr><th>Phòng</th><th class="num">Hóa đơn đang nợ</th><th class="num">Tổng nợ</th></tr></thead><tbody>' +
    (d.length ? d.map(x =>
      '<tr><td><b>' + esc(x.roomName) + '</b></td><td class="num">' + x.openInvoices + '</td><td class="num"><b>' + vnd(x.debtAmount) + '</b></td></tr>').join('') : emptyRow(3)) +
    '</tbody></table>';
}
async function revLoad() {
  const from = (val('rp_from') || '') + '-01', to = (val('rp_to') || '') + '-28';
  const rows = await api.get('/reports/revenue?from=' + from + '&to=' + to);
  const max = Math.max(1, ...rows.map(r => r.revenue));
  const el = document.getElementById('rev_chart');
  if (!el) return;
  el.innerHTML = '<div class="bars">' + rows.map(r => {
    const h = Math.max(3, Math.round((r.revenue / max) * 100));
    return '<div class="bar"><span class="val">' + fmtK(r.revenue) + '</span><div class="fill" style="height:' + h + '%"></div><span class="lbl">' + esc(r.month) + '</span></div>';
  }).join('') + '</div>';
  document.getElementById('rev_table').innerHTML =
    '<table><thead><tr><th>Tháng</th><th class="num">Doanh thu</th></tr></thead><tbody>' +
    rows.map(r => '<tr><td>' + esc(r.month) + '</td><td class="num">' + vnd(r.revenue) + '</td></tr>').join('') + '</tbody></table>';
}
function fmtK(x) { const n = Number(x) || 0; return n >= 1e6 ? (n / 1e6).toFixed(1) + 'tr' : (n >= 1000 ? (n / 1e3).toFixed(0) + 'k' : n); }
async function debtLoad() {
  const d = await api.get('/reports/debt');
  document.getElementById('debt_table').innerHTML =
    '<table><thead><tr><th>Hóa đơn</th><th>Phòng</th><th>Người thuê</th><th>Kỳ</th><th>Hạn</th><th class="num">Tổng</th><th class="num">Còn lại</th><th>Trạng thái</th></tr></thead><tbody>' +
    (d.length ? d.map(x =>
      '<tr><td>' + esc(x.invoiceCode) + '</td><td>' + esc(x.roomName) + '</td><td>' + esc(x.tenantName) + '</td><td>' + esc(x.billingMonth) + '</td>' +
      '<td>' + fmtDate(x.dueDate) + '</td><td class="num">' + vnd(x.totalAmount) + '</td><td class="num"><b>' + vnd(x.remaining) + '</b></td><td>' + statusBadge(x.statusName) + '</td></tr>').join('') : emptyRow(8)) +
    '</tbody></table>';
}
async function exportCsv() {
  const from = (val('rp_from') || '') + '-01', to = (val('rp_to') || '') + '-28';
  try { await apiDownload('/reports/revenue/export?from=' + from + '&to=' + to, 'doanh-thu.csv'); toast('Đã xuất CSV'); }
  catch (e) { toast(e.message, 'err'); }
}

/* ================= USERS (admin) ================= */
async function vUsers(view) {
  const [users, roles] = await Promise.all([api.get('/admin/users'), api.get('/admin/roles')]);
  view.innerHTML = pageHead('Quản lý người dùng', '<button class="btn" onclick="userForm()">+ Tạo người dùng</button>') +
    '<div class="card"><table><thead><tr><th>Username</th><th>Họ tên</th><th>Vai trò</th><th>Trạng thái</th><th style="width:210px"></th></tr></thead><tbody>' +
    (users.length ? users.map(u =>
      '<tr><td><b>' + esc(u.username) + '</b></td><td>' + esc(u.fullName) + '</td><td>' + (u.roles || []).map(r => badge(r, 'brand')).join(' ') + '</td>' +
      '<td>' + (u.isActive ? badge('Hoạt động', 'ok') : badge('Bị khóa', 'bad')) + '</td>' +
      '<td><button class="btn gray sm" onclick="userRoles(' + u.id + ')">Gán vai trò</button> ' +
      '<button class="btn ' + (u.isActive ? 'danger' : 'ok') + ' sm" onclick="userToggle(' + u.id + ',' + u.isActive + ')">' + (u.isActive ? 'Khóa' : 'Mở khóa') + '</button></td></tr>').join('') : emptyRow(5)) +
    '</tbody></table></div>';
}
async function userForm() {
  const roles = await api.get('/admin/roles');
  const b = openModal('Tạo người dùng mới',
    '<div class="form-grid">' +
    '<div><label>Username *</label><input id="u_user"></div>' +
    '<div><label>Mật khẩu *</label><input id="u_pass" type="password"></div>' +
    '<div><label>Họ tên *</label><input id="u_name"></div>' +
    '</div><label>Vai trò</label><div id="u_roles">' +
    roles.map(r => '<label style="display:flex;align-items:center;gap:8px;font-weight:normal"><input type="checkbox" class="urc" value="' + r.code + '"' + (r.code === 'Tenant' ? ' checked' : '') + '> ' + esc(r.name) + '</label>').join('') +
    '</div>');
  b.appendChild(modalButtons('<button class="btn" onclick="userCreate()">Tạo</button>'));
}
async function userCreate() {
  const roles = [...document.querySelectorAll('.urc:checked')].map(c => c.value);
  const body = { username: val('u_user'), password: val('u_pass'), fullName: val('u_name'), roles };
  try { await api.post('/admin/users', body); closeModal(); toast('Đã tạo'); route(); }
  catch (e) { toast(e.message, 'err'); }
}
async function userRoles(id) {
  const [roles] = await Promise.all([api.get('/admin/roles')]);
  const users = await api.get('/admin/users');
  const u = users.find(x => x.id === id);
  const b = openModal('Gán vai trò: ' + u.username,
    '<div>' + roles.map(r =>
      '<label style="display:flex;align-items:center;gap:8px;font-weight:normal"><input type="checkbox" class="urx" value="' + r.code + '"' + ((u.roles || []).includes(r.code) ? ' checked' : '') + '> ' + esc(r.name) + '</label>').join('') + '</div>');
  b.appendChild(modalButtons('<button class="btn" onclick="userRolesSave(' + id + ')">Lưu</button>'));
}
async function userRolesSave(id) {
  const roles = [...document.querySelectorAll('.urx:checked')].map(c => c.value);
  try { await api.put('/admin/users/' + id + '/roles', { roles }); closeModal(); toast('Đã lưu vai trò'); route(); }
  catch (e) { toast(e.message, 'err'); }
}
async function userToggle(id, active) {
  try { await api.put('/admin/users/' + id + '/active', { isActive: !active }); toast('Đã cập nhật'); route(); }
  catch (e) { toast(e.message, 'err'); }
}

/* ================= TENANT SCREENS ================= */
async function vMyContracts(view) {
  const list = await api.get('/me/contracts');
  view.innerHTML = pageHead('Hợp đồng của tôi', '') +
    '<div class="card"><table><thead><tr><th>Mã HĐ</th><th>Phòng</th><th>Thời hạn</th><th class="num">Tiền phòng</th><th class="num">Cọc</th><th>Trạng thái</th></tr></thead><tbody>' +
    (list.length ? list.map(c =>
      '<tr><td>' + esc(c.contractCode) + '</td><td><b>' + esc(c.roomName) + '</b></td><td class="small">' + fmtDate(c.startDate) + ' → ' + fmtDate(c.endDate) + '</td>' +
      '<td class="num">' + vnd(c.monthlyRent) + '</td><td class="num">' + vnd(c.deposit) + '</td><td>' + statusBadge(c.statusName) + '</td></tr>').join('') : emptyRow(6)) +
    '</tbody></table></div>';
}
async function vMyInvoices(view) {
  const list = await api.get('/me/invoices');
  view.innerHTML = pageHead('Hóa đơn của tôi', '') +
    '<div class="card"><table><thead><tr><th>Mã</th><th>Phòng</th><th>Kỳ</th><th>Hạn</th><th class="num">Tổng</th><th class="num">Đã trả</th><th class="num">Còn lại</th><th>Trạng thái</th><th></th></tr></thead><tbody>' +
    (list.length ? list.map(i =>
      '<tr><td>' + esc(i.invoiceCode) + '</td><td><b>' + esc(i.roomName) + '</b></td><td>' + esc(i.billingMonth) + '</td><td>' + fmtDate(i.dueDate) + '</td>' +
      '<td class="num">' + vnd(i.totalAmount) + '</td><td class="num">' + vnd(i.paidAmount) + '</td><td class="num"><b>' + vnd(Math.max(0, i.totalAmount - i.paidAmount)) + '</b></td>' +
      '<td>' + statusBadge(i.statusName) + '</td>' +
      '<td><button class="btn gray sm" onclick="myInvoice(' + i.id + ')">Chi tiết</button></td></tr>').join('') : emptyRow(9)) +
    '</tbody></table></div>';
}
async function myInvoice(id) {
  const i = await api.get('/me/invoices/' + id);
  const b = openModal('Hóa đơn ' + i.invoiceCode,
    rowsOf([
      ['Phòng', i.roomName], ['Kỳ', i.billingMonth], ['Hạn', fmtDate(i.dueDate)],
      ['Trạng thái', statusBadge(i.statusName)]
    ]) +
    '<h4>Chi tiết</h4><table><thead><tr><th>Khoản</th><th class="num">Thành tiền</th></tr></thead><tbody>' +
    (i.items || []).map(x => '<tr><td>' + esc(x.name) + (x.unit ? ' <span class="small muted">(' + x.quantity + ' ' + esc(x.unit) + ')</span>' : '') + '</td><td class="num">' + vnd(x.amount) + '</td></tr>').join('') +
    '</tbody></table><div style="text-align:right;font-weight:700;font-size:16px;margin-top:6px">Tổng: ' + vnd(i.totalAmount) + '</div>' +
    '<h4>Thanh toán</h4>' + (i.payments && i.payments.length ?
      '<table><thead><tr><th>Ngày</th><th class="num">Số tiền</th><th>Phương thức</th></tr></thead><tbody>' +
      i.payments.map(p => '<tr><td>' + fmtDate(p.paidAt) + '</td><td class="num">' + vnd(p.amount) + '</td><td>' + esc(p.methodName) + '</td></tr>').join('') + '</tbody></table>'
      : '<div class="empty" style="padding:10px">Chưa thanh toán</div>'));
  b.appendChild(modalButtons(''));
}
async function vMyRepairs(view) {
  const list = await api.get('/me/repair-requests');
  const cons = await api.get('/me/contracts');
  const rooms = cons.filter(c => c.status === 'Active').map(c => ({ id: c.roomId, name: c.roomName }));
  view.innerHTML = pageHead('Yêu cầu sửa chữa', '<button class="btn" onclick="myRepairForm()">+ Gửi yêu cầu</button>') +
    '<div class="card"><table><thead><tr><th>Phòng</th><th>Tiêu đề</th><th>Ngày gửi</th><th>Trạng thái</th><th style="width:110px"></th></tr></thead><tbody>' +
    (list.length ? list.map(r =>
      '<tr><td><b>' + esc(r.roomName) + '</b></td><td>' + esc(r.subject) + '</td><td class="small">' + fmtDate(r.createdAt) + '</td><td>' + statusBadge(r.statusName) + '</td>' +
      '<td>' + (r.status === 'Pending' ? '<button class="btn danger sm" onclick="myRepairCancel(' + r.id + ')">Hủy</button>' : '') + '</td></tr>').join('') : emptyRow(5)) +
    '</tbody></table></div>' +
    (rooms.length ? '' : '<div class="empty">Bạn chưa có phòng nào trong hợp đồng.</div>');
  window._myRepairRooms = rooms;
}
function myRepairForm() {
  const rooms = window._myRepairRooms || [];
  const b = openModal('Gửi yêu cầu sửa chữa',
    '<label>Phòng</label><select id="mr_room">' + selOpts(rooms, null, 'name') + '</select>' +
    '<label>Tiêu đề *</label><input id="mr_subj">' +
    '<label>Mô tả sự cố</label><textarea id="mr_desc" rows="2"></textarea>');
  b.appendChild(modalButtons('<button class="btn" onclick="myRepairCreate()">Gửi</button>'));
}
async function myRepairCreate() {
  try {
    await api.post('/me/repair-requests', { roomId: num('mr_room'), subject: val('mr_subj'), description: val('mr_desc') || null });
    closeModal(); toast('Đã gửi yêu cầu'); route();
  } catch (e) { toast(e.message, 'err'); }
}
async function myRepairCancel(id) {
  if (!confirm('Hủy yêu cầu này?')) return;
  try { await api.post('/me/repair-requests/' + id + '/cancel'); toast('Đã hủy'); route(); }
  catch (e) { toast(e.message, 'err'); }
}

/* ---------- helpers cuối ---------- */
function rowsOf(rows) {
  return '<div style="margin:10px 0">' + rows.map(([k, v]) =>
    '<div class="detail-row"><span>' + esc(k) + '</span><span style="margin-left:18px;text-align:right">' +
    (typeof v === 'string' && /^<span class="badge/.test(v) ? v : esc(v)) + '</span></div>').join('') + '</div>';
}
function todayStr() { return new Date().toISOString().slice(0, 10); }
function monthNow() { const d = new Date(); return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0'); }
function monthShift(n) { const d = new Date(); d.setMonth(d.getMonth() + n); return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0'); }

window.addEventListener('hashchange', route);
route();
