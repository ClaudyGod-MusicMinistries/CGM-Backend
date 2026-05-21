using ClaudyGod.Application.Common.Interfaces;

namespace ClaudyGod.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
