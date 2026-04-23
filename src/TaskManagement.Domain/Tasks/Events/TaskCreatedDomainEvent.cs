using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks.Events;

public sealed record TaskCreatedDomainEvent(TaskId TaskId, Guid OwnerId, string Title) : DomainEvent;
