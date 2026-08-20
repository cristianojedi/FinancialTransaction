using System.Diagnostics;

namespace FinancialTransaction.Worker;

/// <summary>
/// Fonte de Activities (spans) manuais geradas pelo consumo Kafka do Worker.
/// Precisa ser registrada explicitamente no provider de tracing (AddSource) para que os spans sejam exportados.
/// </summary>
public static class WorkerDiagnostics
{
    public const string SourceName = "FinancialTransaction.Worker";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
