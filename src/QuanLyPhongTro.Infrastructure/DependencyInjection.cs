using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Đăng ký DbContext kết nối MySQL (Pomelo).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình ConnectionStrings:Default. Sao chép appsettings.json.example thành appsettings.json "
                + "(hoặc đặt biến môi trường ConnectionStrings__Default) rồi khởi động lại.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

        return services;
    }

    /// <summary>Tạo database (nếu chưa có), đồng bộ schema và seed dữ liệu mẫu. Gọi khi khởi động.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.EnsureCreatedAsync();
        await EnsureSchemaUpToDateAsync(db);

        // Seed__Reset=true: xóa sạch và nạp lại bộ dữ liệu demo (chỉ lần chạy đó).
        var reset = string.Equals(configuration["Seed:Reset"], "true", StringComparison.OrdinalIgnoreCase);
        await DbSeeder.SeedAsync(db, reset);
    }

    /// <summary>
    /// EnsureCreated chỉ tạo schema khi database chưa tồn tại. Với DB đã có từ bản trước,
    /// các bảng bổ sung sau cần được tạo thủ công (idempotent, dùng CREATE TABLE IF NOT EXISTS).
    /// </summary>
    private static async Task EnsureSchemaUpToDateAsync(AppDbContext db)
    {
        const string createRepairRequests = """
            CREATE TABLE IF NOT EXISTS `RepairRequests` (
              `Id` int NOT NULL AUTO_INCREMENT,
              `RoomId` int NOT NULL,
              `TenantId` int NULL,
              `Subject` varchar(150) NOT NULL,
              `Description` varchar(1000) NULL,
              `Status` int NOT NULL,
              `OwnerNote` varchar(1000) NULL,
              `Cost` decimal(18,2) NULL,
              `CreatedByUserId` int NULL,
              `CreatedByName` varchar(100) NULL,
              `CreatedAt` datetime(6) NOT NULL,
              `HandledAt` datetime(6) NULL,
              `HandledByName` varchar(100) NULL,
              CONSTRAINT `PK_RepairRequests` PRIMARY KEY (`Id`),
              KEY `IX_RepairRequests_RoomId` (`RoomId`),
              KEY `IX_RepairRequests_Status` (`Status`),
              KEY `IX_RepairRequests_TenantId` (`TenantId`),
              CONSTRAINT `FK_RepairRequests_Rooms_RoomId` FOREIGN KEY (`RoomId`) REFERENCES `Rooms` (`Id`) ON DELETE RESTRICT,
              CONSTRAINT `FK_RepairRequests_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE SET NULL
            ) CHARACTER SET utf8mb4
            """;
        await db.Database.ExecuteSqlRawAsync(createRepairRequests);
        await EnsureInvoiceMeterColumnsAsync(db);
        await EnsureNotificationsAsync(db);
    }

    /// <summary>
    /// Bảng Notifications + cột xác nhận thanh toán (Payments.Status/ConfirmedAt) cho DB cũ. Idempotent.
    /// </summary>
    private static async Task EnsureNotificationsAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed)
            await conn.OpenAsync();

        async Task<int> ScalarAsync(string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync();
            return result is null or DBNull ? 0 : Convert.ToInt32(result);
        }

        var hasStatus = await ScalarAsync(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Payments' AND COLUMN_NAME = 'Status'");

        if (wasClosed)
            await conn.CloseAsync();

        if (hasStatus == 0)
        {
            // 2 = Confirmed: các khoản thanh toán cũ coi như đã được xác nhận.
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `Payments` " +
                "ADD COLUMN `Status` int NOT NULL DEFAULT 2, " +
                "ADD COLUMN `ConfirmedAt` datetime(6) NULL");
        }

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS `Notifications` (
              `Id` int NOT NULL AUTO_INCREMENT,
              `Type` varchar(50) NOT NULL,
              `Title` varchar(200) NOT NULL,
              `Message` varchar(500) NOT NULL,
              `LinkPath` varchar(200) NULL,
              `TargetRole` varchar(20) NOT NULL,
              `TargetUserId` int NULL,
              `ActorUserId` int NULL,
              `IsRead` tinyint(1) NOT NULL DEFAULT 0,
              `CreatedAt` datetime(6) NOT NULL,
              CONSTRAINT `PK_Notifications` PRIMARY KEY (`Id`),
              KEY `IX_Notifications_TargetRole_IsRead` (`TargetRole`, `IsRead`)
            ) CHARACTER SET utf8mb4
            """);
    }

    /// <summary>
    /// Gộp chỉ số điện/nước vào hóa đơn: thêm 4 cột chỉ số cho bảng Invoices, chuyển dữ liệu
    /// từ bảng MeterReadings (nếu còn) rồi bỏ bảng này. Idempotent, giữ nguyên dữ liệu hiện có.
    /// </summary>
    private static async Task EnsureInvoiceMeterColumnsAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed)
            await conn.OpenAsync();

        async Task<int> ScalarAsync(string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync();
            return result is null or DBNull ? 0 : Convert.ToInt32(result);
        }

        var hasColumns = await ScalarAsync(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Invoices' AND COLUMN_NAME = 'ElectricOldIndex'");
        var hasReadingsTable = await ScalarAsync(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'MeterReadings'");

        if (wasClosed)
            await conn.CloseAsync();

        if (hasColumns == 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `Invoices` " +
                "ADD COLUMN `ElectricOldIndex` decimal(18,2) NOT NULL DEFAULT 0, " +
                "ADD COLUMN `ElectricNewIndex` decimal(18,2) NOT NULL DEFAULT 0, " +
                "ADD COLUMN `WaterOldIndex` decimal(18,2) NOT NULL DEFAULT 0, " +
                "ADD COLUMN `WaterNewIndex` decimal(18,2) NOT NULL DEFAULT 0");
        }

        // Chuyển chỉ số từ bảng MeterReadings (nếu còn) vào từng hóa đơn rồi bỏ bảng cũ.
        if (hasReadingsTable > 0)
        {
            await db.Database.ExecuteSqlRawAsync("""
                UPDATE `Invoices` i
                JOIN `Contracts` c ON c.Id = i.ContractId
                SET
                  i.ElectricOldIndex = COALESCE((SELECT m.ElectricIndex FROM `MeterReadings` m
                      WHERE m.RoomId = c.RoomId AND m.ReadingDate <= STR_TO_DATE(CONCAT(i.BillingMonth,'-01'),'%Y-%m-%d')
                      ORDER BY m.ReadingDate DESC, m.Id DESC LIMIT 1), 0),
                  i.WaterOldIndex = COALESCE((SELECT m.WaterIndex FROM `MeterReadings` m
                      WHERE m.RoomId = c.RoomId AND m.ReadingDate <= STR_TO_DATE(CONCAT(i.BillingMonth,'-01'),'%Y-%m-%d')
                      ORDER BY m.ReadingDate DESC, m.Id DESC LIMIT 1), 0),
                  i.ElectricNewIndex = COALESCE((SELECT m.ElectricIndex FROM `MeterReadings` m
                      WHERE m.RoomId = c.RoomId AND m.ReadingDate <= LAST_DAY(STR_TO_DATE(CONCAT(i.BillingMonth,'-01'),'%Y-%m-%d'))
                      ORDER BY m.ReadingDate DESC, m.Id DESC LIMIT 1), 0),
                  i.WaterNewIndex = COALESCE((SELECT m.WaterIndex FROM `MeterReadings` m
                      WHERE m.RoomId = c.RoomId AND m.ReadingDate <= LAST_DAY(STR_TO_DATE(CONCAT(i.BillingMonth,'-01'),'%Y-%m-%d'))
                      ORDER BY m.ReadingDate DESC, m.Id DESC LIMIT 1), 0)
                """);

            await db.Database.ExecuteSqlRawAsync("DROP TABLE `MeterReadings`");
        }
    }
}
