using TaskManagement.Application.Common.Abstractions;

namespace TaskManagement.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
