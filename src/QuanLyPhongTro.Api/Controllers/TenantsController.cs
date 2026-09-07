using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "Admin,Owner")]
public class TenantsController : BaseController
{
    private readonly TenantService _tenants;

    public TenantsController(TenantService tenants) => _tenants = tenants;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> List([FromQuery] bool? isActive)
        => Ok(await _tenants.ListAsync(isActive));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TenantDto>> Get(int id) => Ok(await _tenants.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantRequest request)
        => Ok(await _tenants.CreateAsync(request));

    /// <summary>Tạo tài khoản đăng nhập (role Tenant) cho hồ sơ người thuê chưa có tài khoản.</summary>
    [HttpPost("{id:int}/account")]
    public async Task<ActionResult<TenantDto>> CreateAccount(int id, [FromBody] CreateTenantAccountRequest request)
        => Ok(await _tenants.CreateAccountAsync(id, request));

    /// <summary>Đặt lại mật khẩu của tài khoản gắn với hồ sơ người thuê (Admin/Chủ trọ thao tác hộ).</summary>
    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        await _tenants.ResetPasswordAsync(id, request);
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TenantDto>> Update(int id, [FromBody] UpdateTenantRequest request)
        => Ok(await _tenants.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _tenants.DeleteAsync(id);
        return NoContent();
    }
}
