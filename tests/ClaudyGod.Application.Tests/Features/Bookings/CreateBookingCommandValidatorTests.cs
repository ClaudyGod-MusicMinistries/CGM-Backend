using ClaudyGod.Application.Features.Bookings.Commands;
using ClaudyGod.Application.Features.Bookings.DTOs;

namespace ClaudyGod.Application.Tests.Features.Bookings;

public class CreateBookingCommandValidatorTests
{
    [Fact]
    public void Booking_without_postal_code_matches_the_public_form_contract()
    {
        var request = new CreateBookingRequest(
            "Test", "Guest", "guest@example.com", "+2348000000000",
            "KE", "Test Ministry", "Ministry", "Church Service",
            "A detailed description of the planned church service.",
            DateTime.UtcNow.AddMonths(1), "1 Test Street", null,
            "Lagos", "Lagos", null, "Nigeria", true);

        var result = new CreateBookingCommandValidator()
            .Validate(new CreateBookingCommand(request));

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }
}
