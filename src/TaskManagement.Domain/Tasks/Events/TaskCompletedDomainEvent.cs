using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks.Events;

public sealed record TaskCompletedDomainEvent(TaskId TaskId, DateTime CompletedAtUtc) : DomainEvent;
