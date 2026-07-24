using Asp.Versioning;
using ClaudyGod.API.Attributes;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Contacts.Commands;
using ClaudyGod.Application.Features.Contacts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/contacts")]
public class ContactController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactController(IMediator mediator) => _mediator = mediator;

    [PublicEndpoint]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Submit(
        [FromBody] SubmitContactRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new SubmitContactCommand(dto.Name, dto.Email, dto.Message), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Message sent successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ContactMessageDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetContactsQuery(page, pageSize, isRead), ct);
        return Ok(ApiResponse<PaginatedResult<ContactMessageDto>>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteContactCommand(id), ct);
        return Ok(ApiResponse.Ok("Message moved to trash."));
    }
}

public record SubmitContactRequest(string Name, string Email, string Message);
