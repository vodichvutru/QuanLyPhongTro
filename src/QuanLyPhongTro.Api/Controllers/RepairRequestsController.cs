using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/repair-requests")]
[Authorize(Roles = "Admin,Owner")]
public class RepairRequestsController : BaseController
{
    private readonly RepairRequestService _repairs;

    public RepairRequestsController(RepairRequestService repairs) => _repairs = repairs;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepairRequestDto>>> List([FromQuery] RepairStatus? status, [FromQuery] int? roomId)
        => Ok(await _repairs.ListAsync(status, roomId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepairRequestDto>> Get(int id) => Ok(await _repairs.GetAsync(id));

    /// <summary>Chủ trọ lập yêu cầu sửa chữa (hoặc thay người thuê lập).</summary>
    [HttpPost]
    public async Task<ActionResult<RepairRequestDto>> Create([FromBody] CreateRepairRequestRequest request)
        => Ok(await _repairs.CreateAsync(CurrentUserId, request));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<RepairRequestDto>> UpdateStatus(int id, [FromBody] UpdateRepairStatusRequest request)
        => Ok(await _repairs.UpdateStatusAsync(id, CurrentUserId, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _repairs.DeleteAsync(id);
        return NoContent();
    }
}
