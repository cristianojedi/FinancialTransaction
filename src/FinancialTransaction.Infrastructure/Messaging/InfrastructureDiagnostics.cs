using System.Diagnostics;

namespace FinancialTransaction.Infrastructure.Messaging;

/// <summary>
/// Fonte de Activities (spans) manuais geradas pela publicação de mensagens Kafka.
/// Precisa ser registrada explicitamente no provider de tracing (AddSource) para que os spans sejam exportados.
/// </summary>
public static class InfrastructureDiagnostics
{
    public const string SourceName = "FinancialTransaction.Infrastructure";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
