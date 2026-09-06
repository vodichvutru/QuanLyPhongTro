using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Đăng nhập bằng tên đăng nhập + mật khẩu.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
        => Ok(await _auth.LoginAsync(request));

    /// <summary>Cấp cặp token mới bằng refresh token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request)
        => Ok(await _auth.RefreshAsync(request));

    /// <summary>Thông tin tài khoản đang đăng nhập.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me() => Ok(await _auth.GetMeAsync(CurrentUserId));
}
