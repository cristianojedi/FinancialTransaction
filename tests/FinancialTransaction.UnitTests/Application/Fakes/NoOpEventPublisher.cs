using FinancialTransaction.Application.Abstractions.Messaging;
using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.UnitTests.Application.Fakes;

public class NoOpEventPublisher : IEventPublisher
{
    public List<IDomainEvent> PublishedEvents { get; } = [];

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
