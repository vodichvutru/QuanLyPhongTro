// ============================================================================
// Load test cho Quản Lý Phòng Trọ — kiểm chứng NFR-PERF-01 (p95 ≤ 500ms @ 100 req)
// Chạy:  k6 run tests/load/k6-loadtest.js
//   hoặc: k6 run -e BASE_URL=http://localhost:8080 -e USERNAME=chutro -e PASSWORD=123456 tests/load/k6-loadtest.js
// Cài k6: https://grafana.com/docs/k6/latest/set-up/install-k6/
// ============================================================================
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const USERNAME = __ENV.USERNAME || 'chutro';
const PASSWORD = __ENV.PASSWORD || '123456';

export const options = {
  scenarios: {
    // Tăng dần lên 100 VU rồi giảm về 0 — đại diện tải bình thường + đỉnh
    ramp: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 25 },
        { duration: '30s', target: 50 },
        { duration: '20s', target: 100 }, // đỉnh 100 concurrent
        { duration: '20s', target: 50 },
        { duration: '10s', target: 0 },
      ],
    },
  },
  thresholds: {
    'http_req_duration': ['p(95)<500'], // NFR-PERF-01
    'http_req_failed': ['rate<0.01'],   // tỉ lệ lỗi < 1%
  },
};

// setup chạy 1 lần: đăng nhập lấy JWT
export function setup() {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ username: USERNAME, password: PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  if (res.status !== 200) {
    throw new Error(`Login failed (${res.status}): ${res.body}`);
  }
  const token = res.json('accessToken');
  return { token };
}

export default function (data) {
  const headers = {
    Authorization: `Bearer ${data.token}`,
    'Content-Type': 'application/json',
  };

  // 1) Danh sách phòng
  const rooms = http.get(`${BASE_URL}/api/rooms`, { headers });
  check(rooms, { 'rooms 200': (r) => r.status === 200 });

  // 2) Danh sách hóa đơn
  const invoices = http.get(`${BASE_URL}/api/invoices`, { headers });
  check(invoices, { 'invoices 200': (r) => r.status === 200 });

  // 3) Danh sách hợp đồng
  const contracts = http.get(`${BASE_URL}/api/contracts`, { headers });
  check(contracts, { 'contracts 200': (r) => r.status === 200 });

  // 4) Dashboard báo cáo (tổng hợp)
  const dash = http.get(`${BASE_URL}/api/reports/dashboard`, { headers });
  check(dash, { 'dashboard 200': (r) => r.status === 200 });

  // 5) Công nợ theo phòng
  const debt = http.get(`${BASE_URL}/api/reports/debt-by-room`, { headers });
  check(debt, { 'debt-by-room 200': (r) => r.status === 200 });

  // Nhịp nghỉ ngắn mô phỏng người dùng thật
  sleep(1);
}
