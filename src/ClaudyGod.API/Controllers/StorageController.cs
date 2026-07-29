using Asp.Versioning;
using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Storage.Commands;
using ClaudyGod.Application.Features.Storage.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

/// <summary>
/// The website's presigned-S3 upload pipeline. Protected by the global admin
/// authorization policy
/// like every other non-public controller — no extra auth attribute needed;
/// services/api's requireAdmin() is the real per-admin authorization boundary
/// upstream of this proxy call.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/storage")]
public class StorageController : ControllerBase
{
    private readonly IMediator _mediator;

    public StorageController(IMediator mediator) => _mediator = mediator;

    [HttpPost("request-upload")]
    public async Task<ActionResult<ApiResponse<PresignedUploadResult>>> RequestUpload(
        [FromBody] RequestUploadRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RequestUploadCommand(dto), ct);
        return Ok(ApiResponse<PresignedUploadResult>.Ok(result));
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<ApiResponse<ConfirmedUploadResult>>> Confirm(
        [FromBody] ConfirmUploadRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmUploadCommand(dto.SessionId), ct);
        return Ok(ApiResponse<ConfirmedUploadResult>.Ok(result));
    }
}
