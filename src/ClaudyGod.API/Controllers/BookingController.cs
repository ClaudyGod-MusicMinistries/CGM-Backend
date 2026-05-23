using ClaudyGod.Application.Common.Models;
using ClaudyGod.Application.Features.Bookings.Commands;
using ClaudyGod.Application.Features.Bookings.DTOs;
using ClaudyGod.Application.Features.Bookings.Queries;
using ClaudyGod.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaudyGod.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/bookings")]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateBookingRequest dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateBookingCommand(dto), ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Booking request submitted successfully."));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<BookingDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] BookingStatus? status = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBookingsQuery(page, pageSize, status), ct);
        return Ok(ApiResponse<PaginatedResult<BookingDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateBookingStatusRequest dto, CancellationToken ct)
    {
        await _mediator.Send(new UpdateBookingStatusCommand(id, dto.Status, dto.AdminNotes), ct);
        return Ok(ApiResponse.Ok("Booking status updated."));
    }
}
