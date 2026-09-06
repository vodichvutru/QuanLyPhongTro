using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Application.Dtos;
using QuanLyPhongTro.Application.Services;

namespace QuanLyPhongTro.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize(Roles = "Admin,Owner")]
public class ContractsController : BaseController
{
    private readonly ContractService _contracts;

    public ContractsController(ContractService contracts) => _contracts = contracts;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContractDto>>> List([FromQuery] int? roomId, [FromQuery] int? tenantId)
        => Ok(await _contracts.ListAsync(roomId, tenantId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> Get(int id) => Ok(await _contracts.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<ContractDto>> Create([FromBody] CreateContractRequest request)
        => Ok(await _contracts.CreateAsync(request));

    [HttpPost("{id:int}/terminate")]
    public async Task<ActionResult<ContractDto>> Terminate(int id, [FromBody] TerminateContractRequest? request)
        => Ok(await _contracts.TerminateAsync(id, request ?? new TerminateContractRequest()));
}
