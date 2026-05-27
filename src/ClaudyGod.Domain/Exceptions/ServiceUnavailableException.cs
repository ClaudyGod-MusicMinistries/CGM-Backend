namespace ClaudyGod.Domain.Exceptions;

/// <summary>
/// Thrown when a required external service (Anthropic, Paystack, etc.)
/// is not configured or temporarily unreachable. Maps to HTTP 503.
/// </summary>
public sealed class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string service)
        : base($"The {service} service is not available at this time. Please try again later or contact support.")
    {
    }

    public ServiceUnavailableException(string service, string detail)
        : base(detail)
    {
    }
}
