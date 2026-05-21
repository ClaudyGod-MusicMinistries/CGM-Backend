namespace ClaudyGod.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }

    public NotFoundException(string message) : base(message) { }
}
