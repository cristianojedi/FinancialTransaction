using System.Diagnostics;

namespace FinancialTransaction.Application.Common.Telemetry;

/// <summary>
/// Fonte de Activities (spans) manuais gerados pela camada Application.
/// Precisa ser registrada explicitamente no provider de tracing (AddSource) para que os spans sejam exportados.
/// </summary>
public static class ApplicationDiagnostics
{
    public const string SourceName = "FinancialTransaction.Application";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
