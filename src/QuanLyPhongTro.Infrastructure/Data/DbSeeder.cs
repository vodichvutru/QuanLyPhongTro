using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Core.Entities;

namespace QuanLyPhongTro.Infrastructure.Data;

/// <summary>
/// Seed dữ liệu khởi tạo: 3 vai trò, 3 tài khoản mẫu và một vài phòng/người thuê
/// để demo. Chỉ chạy khi bảng tương ứng rỗng.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // ---- Roles ----
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Code = "Admin", Name = "Quản trị viên", Description = "Quản lý user, role, toàn hệ thống" },
                new Role { Code = "Owner", Name = "Chủ trọ", Description = "Quản lý phòng, người thuê, hợp đồng, hóa đơn" },
                new Role { Code = "Tenant", Name = "Người thuê", Description = "Tra cứu hợp đồng, hóa đơn của bản thân" });
            await db.SaveChangesAsync();
        }

        // ---- Users mẫu ----
        var adminRole = await db.Roles.SingleAsync(r => r.Code == "Admin");
        var ownerRole = await db.Roles.SingleAsync(r => r.Code == "Owner");
        var tenantRole = await db.Roles.SingleAsync(r => r.Code == "Tenant");

        if (!await db.Users.AnyAsync())
        {
            var admin = CreateUser("admin", "123456", "Quản trị viên");
            var owner = CreateUser("chutro", "123456", "Trần Phong");
            var tenant = CreateUser("tenant1", "123456", "Nguyễn Văn Thuê");

            admin.UserRoles.Add(new UserRole { Role = adminRole });
            owner.UserRoles.Add(new UserRole { Role = ownerRole });
            // Tenant role dùng chung ở tài khoản tenant1 dưới đây

            db.Users.AddRange(admin, owner);

            // Tạo một người thuê mẫu liên kết tài khoản tenant1
            var ten = new Tenant
            {
                FullName = "Nguyễn Văn Thuê",
                Phone = "0912345678",
                IdentityNumber = "001203001234",
                Email = "tenant1@example.com",
                Address = "Hà Nội",
                User = tenant
            };
            tenant.UserRoles.Add(new UserRole { Role = tenantRole });
            db.Tenants.Add(ten);

            await db.SaveChangesAsync();
        }

        // ---- Phòng mẫu ----
        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.AddRange(
                new Room { Name = "P101", Floor = "1", Area = 20, Price = 2500000, MaxPeople = 2, Status = Core.Enums.RoomStatus.Available },
                new Room { Name = "P102", Floor = "1", Area = 25, Price = 3000000, MaxPeople = 3, Status = Core.Enums.RoomStatus.Available },
                new Room { Name = "P201", Floor = "2", Area = 22, Price = 2800000, MaxPeople = 2, Status = Core.Enums.RoomStatus.Available });
            await db.SaveChangesAsync();
        }
    }

    private static User CreateUser(string username, string password, string fullName)
    {
        return new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            IsActive = true
        };
    }
}
