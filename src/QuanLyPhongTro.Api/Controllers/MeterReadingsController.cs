using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/meter-readings")]
[Authorize(Roles = "Admin,Owner")]
public class MeterReadingsController : BaseController
{
    private readonly MeterReadingService _readings;

    public MeterReadingsController(MeterReadingService readings) => _readings = readings;

    [HttpGet("room/{roomId:int}")]
    public async Task<ActionResult<IReadOnlyList<MeterReadingDto>>> ListByRoom(int roomId)
        => Ok(await _readings.ListByRoomAsync(roomId));

    [HttpPost]
    public async Task<ActionResult<MeterReadingDto>> Create([FromBody] CreateMeterReadingRequest request)
        => Ok(await _readings.CreateAsync(request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _readings.DeleteAsync(id);
        return NoContent();
    }
}
