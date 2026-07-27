using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Trash.Commands;
using ClaudyGod.Application.Features.Trash.DTOs;
using ClaudyGod.Application.Features.Trash.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trash")]
public class TrashController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrashController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<TrashItemDto>>>> GetAll(
        [FromQuery] TrashEntityType? entityType = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTrashQuery(entityType, page, pageSize), ct);
        return Ok(ApiResponse<PaginatedResult<TrashItemDto>>.Ok(result));
    }

    [HttpPost("{entityType}/{id:guid}/restore")]
    public async Task<ActionResult<ApiResponse>> Restore(
        TrashEntityType entityType, Guid id, CancellationToken ct)
    {
        await _mediator.Send(new RestoreTrashItemCommand(entityType, id), ct);
        return Ok(ApiResponse.Ok("Item restored."));
    }

    [HttpDelete("{entityType}/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(
        TrashEntityType entityType, Guid id, CancellationToken ct)
    {
        await _mediator.Send(new PermanentlyDeleteTrashItemCommand(entityType, id), ct);
        return Ok(ApiResponse.Ok("Item permanently deleted."));
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse>> EmptyTrash(CancellationToken ct)
    {
        await _mediator.Send(new EmptyTrashCommand(), ct);
        return Ok(ApiResponse.Ok("Trash emptied."));
    }
}
