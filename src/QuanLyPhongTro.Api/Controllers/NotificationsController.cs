using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : BaseController
{
    private readonly NotificationService _notifications;

    public NotificationsController(NotificationService notifications) => _notifications = notifications;

    /// <summary>Vai trò của tài khoản đang đăng nhập — dùng để lọc thông báo nhận được.</summary>
    private IReadOnlyList<string> Roles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List()
        => Ok(await _notifications.ListAsync(Roles, CurrentUserId));

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount()
        => Ok(await _notifications.UnreadCountAsync(Roles, CurrentUserId));

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notifications.MarkReadAsync(id, Roles, CurrentUserId);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(Roles, CurrentUserId);
        return NoContent();
    }
}
