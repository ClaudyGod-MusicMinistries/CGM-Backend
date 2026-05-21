namespace ClaudyGod.Domain.ValueObjects;

public record ShippingInfo(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Address,
    string City,
    string State,
    string Country,
    string? NearestLocation
);
