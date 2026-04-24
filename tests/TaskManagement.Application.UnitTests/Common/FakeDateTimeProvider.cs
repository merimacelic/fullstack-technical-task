using TaskManagement.Application.Common.Abstractions;

namespace TaskManagement.Application.UnitTests.Common;

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }
}
