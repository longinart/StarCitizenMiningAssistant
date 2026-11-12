using StarCitizenMiningAssistant.Core.Diagnostics;

namespace StarCitizenMiningAssistant.Infrastructure.Diagnostics;

public sealed class NullDiagnosticLog : IDiagnosticLog
{
    public static NullDiagnosticLog Instance { get; } = new();

    private NullDiagnosticLog()
    {
    }

    public void Information(string message)
    {
    }

    public void Warning(string message)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}
