import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Frontend React (Quản lý phòng trọ)
// - dev:  npm run dev   → http://localhost:5173 (proxy /api sang API ASP.NET ở 5255)
// - build: npm run build → xuất thẳng vào wwwroot của API để ASP.NET Core phục vụ
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5255',
    },
  },
  build: {
    outDir: '../src/QuanLyPhongTro.Api/wwwroot',
    emptyOutDir: true,
  },
})
