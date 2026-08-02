using FinancialTransaction.Domain.Common;

namespace FinancialTransaction.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
