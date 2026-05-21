namespace ClaudyGod.Domain.ValueObjects;

public record Address(
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    string Country
);
