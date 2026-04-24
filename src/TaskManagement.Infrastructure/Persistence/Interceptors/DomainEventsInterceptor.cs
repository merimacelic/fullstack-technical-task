using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using TaskManagement.Domain.Common;

namespace TaskManagement.Infrastructure.Persistence.Interceptors;

// Walks tracked aggregates after SaveChanges, logs any raised domain events, and
// clears them. A future iteration can wire an IPublisher here to hand events to
// Mediator's notification pipeline.
public sealed class DomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<DomainEventsInterceptor> _logger;

    public DomainEventsInterceptor(ILogger<DomainEventsInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        var aggregates = context.ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                _logger.LogInformation(
                    "Domain event raised: {EventType} (EventId={EventId})",
                    domainEvent.GetType().Name,
                    domainEvent.EventId);
            }

            aggregate.ClearDomainEvents();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
