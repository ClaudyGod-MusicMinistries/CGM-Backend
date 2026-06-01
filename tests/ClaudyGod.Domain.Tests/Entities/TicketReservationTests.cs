using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Enums;
using ClaudyGod.Domain.Exceptions;
using FluentAssertions;

namespace ClaudyGod.Domain.Tests.Entities;

public class TicketReservationTests
{
    [Fact]
    public void Create_SetsStatusReserved()
    {
        var ticket = MakeTicket();
        ticket.Status.Should().Be(TicketStatus.Reserved);
    }

    [Fact]
    public void CheckIn_ChangesStatusToCheckedIn()
    {
        var ticket = MakeTicket();
        ticket.CheckIn("admin@claudygod.org");

        ticket.Status.Should().Be(TicketStatus.CheckedIn);
        ticket.CheckedInAt.Should().NotBeNull();
        ticket.CheckedInBy.Should().Be("admin@claudygod.org");
    }

    [Fact]
    public void CheckIn_ThrowsIfAlreadyCancelled()
    {
        var ticket = MakeTicket();
        ticket.Cancel();

        var act = () => ticket.CheckIn("admin");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var ticket = MakeTicket();
        ticket.Cancel();
        ticket.Status.Should().Be(TicketStatus.Cancelled);
    }

    private static TicketReservation MakeTicket() =>
        TicketReservation.Create(Guid.NewGuid(), "John", "Doe",
            "john@example.com", "+2348000000000", 2, "CGM-ABC123");
}
