# Hướng dẫn cài đặt & triển khai

## 1. Yêu cầu

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (đã thử nghiệm trên .NET 8)
- MySQL **8.0+** đang chạy (mặc định `127.0.0.1:3306`)

## 2. Chạy trong môi trường phát triển

```bash
# (tùy chọn) chỉnh chuỗi kết nối + JWT trong:
# src/QuanLyPhongTro.Api/appsettings.json  → "ConnectionStrings:Default", "Jwt"

dotnet run --project src/QuanLyPhongTro.Api
```

Khởi động sẽ **tự tạo database + seed dữ liệu mẫu** (không cần chạy migration).

| Địa chỉ | Nội dung |
|---------|----------|
| http://localhost:5255 | Giao diện web (SPA) |
| http://localhost:5255/swagger | Tài liệu API |

Chạy unit test:

```bash
dotnet test
```

## 3. Đóng gói (production)

```bash
dotnet publish src/QuanLyPhongTro.Api -c Release -o publish
./publish/QuanLyPhongTro.Api.exe        # chạy, hoặc dùng IIS/service
```

Trước khi đưa lên production cần:

1. **Cấu hình lại** `appsettings.json`:
   - `ConnectionStrings:Default` → server/DB/user/password thật của môi trường deploy.
   - `Jwt:Secret` → **khóa bí mật mới, ≥ 32 ký tự, không dùng giá trị mặc định**.
2. **Ẩn seed**: tài khoản dân cư demo sinh ra khi bảng `Users` rỗng. Muốn sạch dữ liệu mẫu trong production, xóa `DbSeeder` trước khi build hoặc xóa dữ liệu seed khỏi CSDL.
3. Bật HTTPS (reverse proxy nginx/Caddy, hoặc chứng chỉ trong Kestrel).
4. Cấu hình backup CSDL định kỳ (ví dụ `mysqldump`).

## 4. Cấu hình quan trọng

| Key | Mặc định | Ghi chú |
|-----|----------|---------|
| `ConnectionStrings:Default` | `Server=127.0.0.1;Port=3306;Database=quanlyphongtro;User=root;Password=<DB_PASSWORD>` | Chuỗi kết nối MySQL (thay `<DB_PASSWORD>` bằng mật khẩu thật, đừng commit) |
| `Jwt:Secret` | `QuanLyPhongTro_Secret_Key_Do_Not_Share_...` | Phải ≥ 32 ký tự, đổi khi deploy |
| `Jwt:AccessTokenExpiryMinutes` | `1440` (24h) | Thời hạn access token |
| `Jwt:RefreshTokenExpiryDays` | `7` | Thời hạn refresh token |

## 5. Khắc phục sự cố thường gặp

- **"Không kết nối được MySQL"** (nhìn log khi chạy): kiểm tra service MySQL đã bật, đúng port 3306 và chuỗi kết nối.
- **Muốn reset toàn bộ dữ liệu**: dừng app → xóa database `quanlyphongtro` → chạy lại app (tự tạo + seed).
- **Thêm bảng mới sau khi DB đã tồn tại**: cập nhật `EnsureSchemaUpToDateAsync()` trong `src/QuanLyPhongTro.Infrastructure/DependencyInjection.cs` (bổ sung `CREATE TABLE IF NOT EXISTS` tương ứng), vì bản hiện tại dùng `EnsureCreated` chứ không dùng EF Migration.
