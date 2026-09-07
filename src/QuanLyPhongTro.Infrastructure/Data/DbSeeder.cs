using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Infrastructure.Data;

/// <summary>
/// Seed dữ liệu demo đầy đủ:
///   - Admin (quản trị), Chủ trọ (Owner) và nhiều người thuê, mỗi người thuê đều có TÀI KHOẢN
///     (đăng nhập để xem hợp đồng + hóa đơn của riêng mình).
///   - Nhiều phòng, hợp đồng hoạt động + lịch sử, chỉ số điện/nước nhiều tháng,
///     ~30 hóa đơn trải 5 tháng (trạng thái đa dạng), thanh toán và yêu cầu sửa chữa.
///
/// Reset toàn bộ dữ liệu về đúng bộ demo: đặt biến môi trường Seed__Reset=true khi chạy
/// (chỉ lần đó; những lần chạy sau bình thường sẽ KHÔNG xóa dữ liệu).
/// </summary>
public static class DbSeeder
{
    // Tài khoản/người thuê demo — tên tài khoản (username) = tên người thuê (không dấu, không khoảng trắng)
    private static readonly (string User, string FullName, string Phone, string Idn, string Email)[] Tenants =
    {
        ("nguyenvana",   "Nguyễn Văn An",   "0912 001 001", "001202000001", "an@example.com"),
        ("tranthibinh",  "Trần Thị Bình",   "0912 001 002", "001202000002", "binh@example.com"),
        ("levancuong",   "Lê Văn Cường",    "0912 001 003", "001202000003", "cuong@example.com"),
        ("phamthidung",  "Phạm Thị Dung",   "0912 001 004", "001202000004", "dung@example.com"),
        ("hoangvanem",   "Hoàng Văn Em",    "0912 001 005", "001202000005", "em@example.com"),
        ("dothiphuong",  "Đỗ Thị Phương",   "0912 001 006", "001202000006", "phuong@example.com"),
        ("vuquanghoa",   "Vũ Quang Hoa",    "0912 001 007", "001202000007", "hoa@example.com"),
    };

    // Phòng: (Name, Floor, Area, Price, Max, cần cho thuê?)
    private static readonly (string Name, string Floor, decimal Area, decimal Price, int Max, bool Rent)[] Rooms =
    {
        ("P101", "1", 20, 2_500_000, 2, true),
        ("P102", "1", 25, 2_800_000, 3, true),
        ("P103", "1", 18, 2_200_000, 2, true),
        ("P104", "1", 22, 2_400_000, 3, true),
        ("P105", "1", 28, 3_000_000, 3, true),
        ("P201", "2", 26, 3_200_000, 3, true),
        ("P202", "2", 30, 3_500_000, 4, true),
        ("P203", "2", 24, 2_900_000, 3, false),
        ("P204", "2", 22, 2_700_000, 2, false),
        ("P301", "3", 32, 3_800_000, 4, false),
    };

    // Hợp đồng hiện tại (theo thứ tự phòng đang cho thuê). Offset tháng bắt đầu so với cửa sổ báo cáo.
    private static readonly int[] StartOffsets = { 0, 0, 1, 1, 2, 3, 3 };

    public static async Task SeedAsync(AppDbContext db, bool reset = false)
    {
        if (!reset && await db.Users.AnyAsync())
            return; // đã có dữ liệu, không đè lên

        if (reset)
            await WipeAsync(db);

        // ---------- 1. Roles ----------
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Code = "Admin", Name = "Quản trị viên", Description = "Quản lý user, role, toàn hệ thống" },
                new Role { Code = "Owner", Name = "Chủ trọ", Description = "Quản lý phòng, người thuê, hợp đồng, hóa đơn, thu tiền" },
                new Role { Code = "Tenant", Name = "Người thuê", Description = "Xem hợp đồng, hóa đơn của bản thân, gửi yêu cầu sửa chữa" });
            await db.SaveChangesAsync();
        }
        var adminRole = await db.Roles.SingleAsync(r => r.Code == "Admin");
        var ownerRole = await db.Roles.SingleAsync(r => r.Code == "Owner");
        var tenantRole = await db.Roles.SingleAsync(r => r.Code == "Tenant");

        // ---------- 2. Users + Tenants ----------
        var admin = CreateUser("admin", "123456", "Quản trị viên");
        var owner = CreateUser("chutro", "123456", "Trần Phong (chủ trọ)");
        admin.UserRoles.Add(new UserRole { Role = adminRole });
        owner.UserRoles.Add(new UserRole { Role = ownerRole });
        db.Users.AddRange(admin, owner);

        var tenants = new List<Tenant>();
        var tenantUsers = new List<User>();
        for (var i = 0; i < Tenants.Length; i++)
        {
            var t = Tenants[i];
            var u = CreateUser(t.User, "123456", t.FullName);
            u.UserRoles.Add(new UserRole { Role = tenantRole });
            var tenant = new Tenant
            {
                FullName = t.FullName,
                Phone = t.Phone,
                IdentityNumber = t.Idn,
                Email = t.Email,
                Address = $"TP.HCM - khu vực {Rooms[i].Floor}",
                IsActive = true,
                User = u
            };
            tenantUsers.Add(u);
            tenants.Add(tenant);
            db.Users.Add(u);
            db.Tenants.Add(tenant);
        }
        await db.SaveChangesAsync();

        // ---------- 3. Rooms ----------
        var roomEntities = new List<Room>();
        for (var i = 0; i < Rooms.Length; i++)
        {
            var r = Rooms[i];
            roomEntities.Add(new Room
            {
                Name = r.Name,
                Floor = r.Floor,
                Area = r.Area,
                Price = r.Price,
                MaxPeople = r.Max,
                Status = r.Rent ? RoomStatus.Rented : RoomStatus.Available
            });
        }
        db.Rooms.AddRange(roomEntities);
        await db.SaveChangesAsync();

        // ---------- 4. Contracts ----------
        var today = DateTime.Today;
        var winStart = new DateTime(today.Year, today.Month, 1).AddMonths(-4); // cửa sổ báo cáo 5 tháng

        var rented = roomEntities.Where(r => r.Status == RoomStatus.Rented).ToList();
        var contracts = new List<Contract>();
        for (var i = 0; i < rented.Count; i++)
        {
            var start = winStart.AddMonths(StartOffsets[i]);
            var room = rented[i];
            var contract = new Contract
            {
                ContractCode = $"HD-{room.Name}-{start:yyyy}",
                RoomId = room.Id,
                TenantId = tenants[i].Id,
                StartDate = start,
                EndDate = start.AddYears(1).AddDays(-1),
                MonthlyRent = room.Price,
                Deposit = room.Price,
                ElectricPrice = 3_500,
                WaterPrice = 25_000,
                Status = ContractStatus.Active,
                Note = null
            };
            contracts.Add(contract);
            db.Contracts.Add(contract);
        }
        await db.SaveChangesAsync();

        // ---------- 5. Meter readings (cuối tháng, từ trước cửa sổ đến hết tháng trước) ----------
        // Điện/nước tích lũy tăng dần, để "tạo hóa đơn" sau này vẫn tính được tiêu thụ.
        for (var i = 0; i < rented.Count; i++)
        {
            var room = rented[i];
            var baseElec = 320 + i * 97m;
            var baseWater = 18 + i * 9m;
            var dE = 96 + (i * 23) % 70;   // kWh/tháng
            var dW = 6 + (i * 4) % 6;      // m³/tháng
            for (var off = -1; off <= 4; off++) // tháng từ winStart-1 đến winStart+4 (tức tháng trước)
            {
                var boundary = winStart.AddMonths(off);
                var lastDay = new DateTime(boundary.Year, boundary.Month, DateTime.DaysInMonth(boundary.Year, boundary.Month));
                if (lastDay >= today) continue;
                baseElec += dE;
                baseWater += dW;
                db.MeterReadings.Add(new MeterReading
                {
                    RoomId = room.Id,
                    ReadingDate = lastDay,
                    ElectricIndex = decimal.Round(baseElec, 2),
                    WaterIndex = decimal.Round(baseWater, 2),
                    Note = $"Chốt cuối tháng {lastDay:MM/yyyy}"
                });
            }
        }
        await db.SaveChangesAsync();

        // ---------- 6. Invoices + Payments ----------
        await SeedInvoicesAndPaymentsAsync(db, contracts, rented, tenants, today, winStart);

        // ---------- 7. Repair requests ----------
        await SeedRepairsAsync(db, roomEntities, tenants, tenantUsers);

        db.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS=1");
    }

    private static async Task SeedInvoicesAndPaymentsAsync(
        AppDbContext db, List<Contract> contracts, List<Room> rented,
        List<Tenant> tenants, DateTime today, DateTime winStart)
    {
        // 2 pha: lưu hóa đơn trước để có Id, rồi mới tạo thanh toán tham chiếu InvoiceId.
        var pendingPayments = new List<(Invoice Inv, decimal Amount, PaymentMethod Method, DateTime PaidAt, string Note)>();

        for (var i = 0; i < contracts.Count; i++)
        {
            var contract = contracts[i];
            var room = rented[i];
            var dE = 96 + (i * 23) % 70;   // kWh
            var dW = 6 + (i * 4) % 6;      // m³
            var hasInternet = i % 2 == 0;  // một nửa phòng có phí internet
            var month = new DateTime(contract.StartDate.Year, contract.StartDate.Month, 1);
            var endMonth = new DateTime(today.Year, today.Month, 1);
            decimal carried = 0;

            while (month <= endMonth)
            {
                var label = month.ToString("yyyy-MM");
                var monthEnd = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                var isCurrent = month.Year == today.Year && month.Month == today.Month;

                // Dòng chi tiết
                var items = new List<InvoiceItem> { new() { Name = $"Tiền phòng {room.Name}", Quantity = 1, UnitPrice = contract.MonthlyRent, Amount = contract.MonthlyRent } };
                if (!isCurrent) // tháng hiện tại chưa chốt điện/nước
                {
                    items.Add(new InvoiceItem { Name = "Tiền điện", Quantity = dE, Unit = "kWh", UnitPrice = contract.ElectricPrice, Amount = dE * contract.ElectricPrice });
                    items.Add(new InvoiceItem { Name = "Tiền nước", Quantity = dW, Unit = "m³", UnitPrice = contract.WaterPrice, Amount = dW * contract.WaterPrice });
                }
                if (hasInternet)
                    items.Add(new InvoiceItem { Name = "Internet", Quantity = 1, UnitPrice = 150_000, Amount = 150_000 });

                var total = decimal.Round(items.Sum(x => x.Amount), 2);

                // Xác định trạng thái / tỷ lệ đã thanh toán
                var ratio = OlderMonthRatio(i, month, winStart); // các tháng trước mặc định trả đủ
                if (isCurrent)
                    ratio = CurrentMonthRatio(i);

                var paid = decimal.Round(total * ratio, 2);
                var status = paid >= total - 0.01m ? InvoiceStatus.Paid : (paid > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid);

                var invoice = new Invoice
                {
                    InvoiceCode = $"HD-{label.Replace("-", "")}-{room.Name}",
                    ContractId = contract.Id,
                    BillingMonth = label,
                    IssueDate = month,
                    DueDate = monthEnd,
                    TotalAmount = total,
                    PaidAmount = paid,
                    PreviousDebt = carried,
                    Status = status,
                    Items = items
                };
                db.Invoices.Add(invoice);

                if (paid > 0)
                    pendingPayments.Add((invoice, paid, (PaymentMethod)((i + month.Month) % 3 + 1), PickPaymentDate(i, month, today, winStart), $"Thanh toán tiền {label}"));

                carried += Math.Max(0, total - paid); // công nợ chuyển kỳ sau
                month = month.AddMonths(1);
            }
        }

        await db.SaveChangesAsync(); // hóa đơn + dòng chi tiết đã có Id

        foreach (var p in pendingPayments)
        {
            db.Payments.Add(new Payment
            {
                InvoiceId = p.Inv.Id,
                Amount = p.Amount,
                Method = p.Method,
                PaidAt = p.PaidAt,
                Note = p.Note
            });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Tỷ lệ thanh toán các tháng trước: trả đủ, trừ phòng P102 (index 1) còn nợ tháng gần nhất.</summary>
    private static decimal OlderMonthRatio(int idx, DateTime month, DateTime winStart)
    {
        if (idx == 1 && month == winStart.AddMonths(3)) return 0; // P102 chưa trả tháng trước tháng này → tạo công nợ
        return 1;
    }

    /// <summary>Tỷ lệ thanh toán tháng hiện tại — đa dạng: trả đủ / trả một phần / chưa trả.</summary>
    private static decimal CurrentMonthRatio(int idx) => idx switch
    {
        0 => 1,     // P101 trả đủ
        2 => 0.5m,  // P103 trả một nửa
        3 => 0.3m,  // P104 trả 30%
        6 => 1,     // P202 trả đủ
        _ => 0      // P102, P105, P201 chưa trả
    };

    private static DateTime PickPaymentDate(int idx, DateTime month, DateTime today, DateTime winStart)
    {
        // Phòng đầu trả sớm trong tháng để lấp tháng đầu của biểu đồ; còn lại trả sau khi hết hạn.
        var baseDate = (idx == 0 && month == winStart)
            ? new DateTime(month.Year, month.Month, 8)
            : new DateTime(month.Year, month.Month, 1).AddMonths(1).AddDays(1 + idx);
        return baseDate > today ? today : baseDate;
    }

    private static async Task SeedRepairsAsync(AppDbContext db, List<Room> rooms, List<Tenant> tenants, List<User> tenantUsers)
    {
        var byName = rooms.ToDictionary(r => r.Name);
        var room = byName["P101"];
        var tenant = tenants[0];
        db.RepairRequests.Add(new RepairRequest
        {
            RoomId = room.Id, TenantId = tenant.Id,
            Subject = "Vòi nước phòng tắm bị rò",
            Description = "Vòi sen nhỏ giọt suốt đêm, đề nghị thay gioăng.",
            Status = RepairStatus.Pending,
            CreatedByName = tenantUsers[0].FullName, CreatedAt = DateTime.Now.AddDays(-1)
        });

        room = byName["P102"]; tenant = tenants[1];
        db.RepairRequests.Add(new RepairRequest
        {
            RoomId = room.Id, TenantId = tenant.Id,
            Subject = "Điều hòa không mát",
            Description = "Điều hòa chạy nhưng gió yếu, nghi thiếu ga.",
            Status = RepairStatus.InProgress,
            OwnerNote = "Đã đặt thợ kiểm tra ga, hẹn cuối tuần.",
            Cost = 350_000,
            CreatedByName = tenantUsers[1].FullName, CreatedAt = DateTime.Now.AddDays(-4),
            HandledByName = "Trần Phong (chủ trọ)", HandledAt = DateTime.Now.AddDays(-2)
        });

        room = byName["P103"]; tenant = tenants[2];
        db.RepairRequests.Add(new RepairRequest
        {
            RoomId = room.Id, TenantId = tenant.Id,
            Subject = "Bóng đèn phòng khách nhấp nháy",
            Status = RepairStatus.Approved,
            OwnerNote = "Sẽ thay bóng mới trong ngày.",
            CreatedByName = tenantUsers[2].FullName, CreatedAt = DateTime.Now.AddDays(-2),
            HandledByName = "Trần Phong (chủ trọ)", HandledAt = DateTime.Now.AddDays(-1)
        });

        room = byName["P105"]; tenant = tenants[4];
        db.RepairRequests.Add(new RepairRequest
        {
            RoomId = room.Id, TenantId = tenant.Id,
            Subject = "Cửa sổ khó đóng",
            Description = "Ray cửa kẹt, đóng rất mạnh mới khít.",
            Status = RepairStatus.Completed,
            OwnerNote = "Đã tra dầu và chỉnh ray cửa.",
            Cost = 120_000,
            CreatedByName = tenantUsers[4].FullName, CreatedAt = DateTime.Now.AddDays(-9),
            HandledByName = "Trần Phong (chủ trọ)", HandledAt = DateTime.Now.AddDays(-7)
        });

        room = byName["P202"]; tenant = tenants[6];
        db.RepairRequests.Add(new RepairRequest
        {
            RoomId = room.Id, TenantId = tenant.Id,
            Subject = "Ổ cắm điện gần bàn làm việc lỏng",
            Status = RepairStatus.Rejected,
            OwnerNote = "Ổ cắm không thuộc tài sản phòng (do khách tự lắp).",
            CreatedByName = tenantUsers[6].FullName, CreatedAt = DateTime.Now.AddDays(-5),
            HandledByName = "Trần Phong (chủ trọ)", HandledAt = DateTime.Now.AddDays(-3)
        });

        await db.SaveChangesAsync();
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

    /// <summary>Xóa toàn bộ dữ liệu (thứ tự theo khóa ngoại), giữ nguyên cấu trúc bảng.</summary>
    private static async Task WipeAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS=0");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `RepairRequests`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `InvoiceItems`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `Payments`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `Invoices`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `MeterReadings`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `Contracts`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `Tenants`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `UserRoles`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `Rooms`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `Users`");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM `Roles`");
        await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS=1");
    }
}
