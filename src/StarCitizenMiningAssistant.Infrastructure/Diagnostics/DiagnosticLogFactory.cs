using StarCitizenMiningAssistant.Core.Diagnostics;

namespace StarCitizenMiningAssistant.Infrastructure.Diagnostics;

public static class DiagnosticLogFactory
{
    public static IDiagnosticLog CreateDefault()
    {
        try
        {
            return new FileDiagnosticLog(FileDiagnosticLog.DefaultLogPath());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return NullDiagnosticLog.Instance;
        }
    }
}
