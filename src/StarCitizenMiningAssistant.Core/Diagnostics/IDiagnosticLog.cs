namespace StarCitizenMiningAssistant.Core.Diagnostics;

public interface IDiagnosticLog
{
    void Information(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}
