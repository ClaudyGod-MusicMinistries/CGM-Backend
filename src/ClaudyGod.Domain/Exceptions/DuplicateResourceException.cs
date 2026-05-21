namespace ClaudyGod.Domain.Exceptions;

public class DuplicateResourceException : DomainException
{
    public DuplicateResourceException(string message) : base(message) { }
}
