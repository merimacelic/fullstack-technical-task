namespace TaskManagement.Application.Common.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
