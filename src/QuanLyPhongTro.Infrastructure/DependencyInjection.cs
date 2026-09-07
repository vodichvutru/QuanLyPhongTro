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
    }
}
