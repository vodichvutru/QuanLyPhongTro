using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Application.Common;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Core.Entities;
using QuanLyPhongTro.Infrastructure.Data;

namespace QuanLyPhongTro.Application.Services;

/// <summary>Tạo và đọc thông báo trong hệ thống (gửi theo vai trò, hoặc riêng cho một tài khoản).</summary>
public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    /// <summary>Thêm thông báo vào cùng transaction với nghiệp vụ (không tự SaveChanges).
    /// <paramref name="targetUserId"/> != null → chỉ tài khoản đó nhận; null → mọi tài khoản thuộc vai trò.</summary>
    public void Add(string targetRole, string type, string title, string message, string? linkPath,
        int? actorUserId = null, int? targetUserId = null)
    {
        _db.Notifications.Add(new Notification
        {
            TargetRole = targetRole,
            TargetUserId = targetUserId,
            Type = type,
            Title = title,
            Message = message,
            LinkPath = linkPath,
            ActorUserId = actorUserId,
        });
    }

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(IReadOnlyList<string> roles, int userId, int count = 30)
    {
        var list = await _db.Notifications.AsNoTracking()
            .Where(n => n.TargetUserId == userId || (n.TargetUserId == null && roles.Contains(n.TargetRole)))
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Take(Math.Clamp(count, 1, 100))
            .ToListAsync();
        return list.Select(DtoMapper.ToNotification).ToList();
    }

    public Task<int> UnreadCountAsync(IReadOnlyList<string> roles, int userId) =>
        _db.Notifications.CountAsync(n =>
            (n.TargetUserId == userId || (n.TargetUserId == null && roles.Contains(n.TargetRole))) && !n.IsRead);

    public async Task MarkReadAsync(int id, IReadOnlyList<string> roles, int userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x =>
                x.Id == id && (x.TargetUserId == userId || (x.TargetUserId == null && roles.Contains(x.TargetRole))))
            ?? throw new AppException("Không tìm thấy thông báo.", 404);
        if (!n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(IReadOnlyList<string> roles, int userId)
    {
        var items = await _db.Notifications
            .Where(n => (n.TargetUserId == userId || (n.TargetUserId == null && roles.Contains(n.TargetRole))) && !n.IsRead)
            .ToListAsync();
        if (items.Count == 0) return;
        foreach (var n in items) n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}
