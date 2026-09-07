using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

/// <summary>Quản lý User/Role — chỉ Admin.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin) => _admin = admin;

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> ListUsers() => Ok(await _admin.ListUsersAsync());

    [HttpPost("users")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
        => Ok(await _admin.CreateUserAsync(request));

    [HttpPut("users/{id:int}/roles")]
    public async Task<ActionResult<UserDto>> UpdateUserRoles(int id, [FromBody] UpdateUserRolesRequest request)
        => Ok(await _admin.UpdateUserRolesAsync(id, request));

    [HttpPut("users/{id:int}/active")]
    public async Task<ActionResult<UserDto>> SetUserActive(int id, [FromBody] SetUserActiveRequest request)
        => Ok(await _admin.SetUserActiveAsync(id, request));

    [HttpPut("users/{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        await _admin.ResetPasswordAsync(id, request);
        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> ListRoles() => Ok(await _admin.ListRolesAsync());
}
