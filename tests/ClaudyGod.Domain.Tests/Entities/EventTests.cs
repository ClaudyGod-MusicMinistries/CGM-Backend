using ClaudyGod.Domain.Entities;
using ClaudyGod.Domain.Exceptions;
using FluentAssertions;

namespace ClaudyGod.Domain.Tests.Entities;

public class EventTests
{
    [Fact]
    public void IncrementReserved_WithinCapacity_UpdatesAvailability()
    {
        var sut = Event.Create("Conference", DateTime.UtcNow.AddDays(1), totalCapacity: 5);

        sut.IncrementReserved(3);

        sut.ReservedCount.Should().Be(3);
        sut.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public void IncrementReserved_AboveCapacity_RejectsWithoutChangingState()
    {
        var sut = Event.Create("Conference", DateTime.UtcNow.AddDays(1), totalCapacity: 2);

        var act = () => sut.IncrementReserved(3);

        act.Should().Throw<DomainException>();
        sut.ReservedCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IncrementReserved_NonPositiveQuantity_IsRejected(int quantity)
    {
        var sut = Event.Create("Conference", DateTime.UtcNow.AddDays(1), totalCapacity: 2);

        var act = () => sut.IncrementReserved(quantity);

        act.Should().Throw<DomainException>();
    }
}
