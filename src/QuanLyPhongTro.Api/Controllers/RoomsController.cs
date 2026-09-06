using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Core.Enums;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize(Roles = "Admin,Owner")]
public class RoomsController : BaseController
{
    private readonly RoomService _rooms;

    public RoomsController(RoomService rooms) => _rooms = rooms;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> List([FromQuery] RoomStatus? status)
        => Ok(await _rooms.ListAsync(status));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> Get(int id) => Ok(await _rooms.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomRequest request)
        => Ok(await _rooms.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] UpdateRoomRequest request)
        => Ok(await _rooms.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _rooms.DeleteAsync(id);
        return NoContent();
    }
}
