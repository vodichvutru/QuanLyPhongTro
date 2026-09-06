using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Core.Entities;

namespace QuanLyPhongTro.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Users / Roles ----
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.PasswordHash).HasMaxLength(200);
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.Email).HasMaxLength(100);
            e.Property(u => u.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(r => r.Code).IsUnique();
            e.Property(r => r.Code).HasMaxLength(30);
            e.Property(r => r.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Room ----
        modelBuilder.Entity<Room>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Name).HasMaxLength(50);
            e.Property(r => r.Floor).HasMaxLength(20);
            e.Property(r => r.Note).HasMaxLength(500);
        });

        // ---- Tenant ----
        modelBuilder.Entity<Tenant>(e =>
        {
            e.Property(t => t.FullName).HasMaxLength(100);
            e.Property(t => t.Phone).HasMaxLength(20);
            e.Property(t => t.IdentityNumber).HasMaxLength(20);
            e.Property(t => t.Email).HasMaxLength(100);
            e.Property(t => t.Address).HasMaxLength(200);
            e.Property(t => t.Note).HasMaxLength(500);
            e.HasIndex(t => t.IdentityNumber);
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---- Contract ----
        modelBuilder.Entity<Contract>(e =>
        {
            e.HasIndex(c => c.ContractCode).IsUnique();
            e.Property(c => c.ContractCode).HasMaxLength(30);
            e.Property(c => c.Note).HasMaxLength(500);
            e.HasOne(c => c.Room).WithMany(r => r.Contracts).HasForeignKey(c => c.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Tenant).WithMany(t => t.Contracts).HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- MeterReading ----
        modelBuilder.Entity<MeterReading>(e =>
        {
            e.Property(m => m.Note).HasMaxLength(300);
            e.HasOne(m => m.Room).WithMany(r => r.MeterReadings).HasForeignKey(m => m.RoomId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Invoice ----
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceCode).IsUnique();
            e.Property(i => i.InvoiceCode).HasMaxLength(30);
            e.Property(i => i.BillingMonth).HasMaxLength(7);
            e.Property(i => i.Note).HasMaxLength(500);
            e.HasOne(i => i.Contract).WithMany(c => c.Invoices).HasForeignKey(i => i.ContractId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- InvoiceItem ----
        modelBuilder.Entity<InvoiceItem>(e =>
        {
            e.Property(i => i.Name).HasMaxLength(150);
            e.Property(i => i.Unit).HasMaxLength(20);
            e.HasOne(i => i.Invoice).WithMany(i => i.Items).HasForeignKey(i => i.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---- RepairRequest ----
        modelBuilder.Entity<RepairRequest>(e =>
        {
            e.Property(x => x.Subject).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.OwnerNote).HasMaxLength(1000);
            e.Property(x => x.CreatedByName).HasMaxLength(100);
            e.Property(x => x.HandledByName).HasMaxLength(100);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---- Payment ----
        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Reference).HasMaxLength(100);
            e.Property(p => p.Note).HasMaxLength(300);
            e.HasOne(p => p.Invoice).WithMany(i => i.Payments).HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Decimal precision (MySQL không có decimal mặc định tốt) ----
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var prop in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                prop.SetPrecision(18);
                prop.SetScale(2);
            }
        }
    }
}
