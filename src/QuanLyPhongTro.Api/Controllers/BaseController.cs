using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Common;

namespace QuanLyPhongTro.Api.Controllers;

/// <summary>Base controller: đọc userId từ JWT.</summary>
public abstract class BaseController : ControllerBase
{
    protected int CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) is { } id && int.TryParse(id, out var uid)
            ? uid
            : throw new AppException("Không xác định được tài khoản.", 401);
}
