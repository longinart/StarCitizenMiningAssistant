using StarCitizenMiningAssistant.Core.Diagnostics;

namespace StarCitizenMiningAssistant.Infrastructure.Diagnostics;

public sealed class FileDiagnosticLog : IDiagnosticLog
{
    private readonly Lock _gate = new();
    private readonly string _path;

    public FileDiagnosticLog(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _path = path;
    }

    public static string DefaultLogPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "StarCitizenMiningAssistant", "logs", "app.log");
    }

    public void Information(string message) => Write("INFO", message, null);

    public void Warning(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(message);

        var line = $"{DateTimeOffset.Now:O} {level} {message}";
        if (exception is not null)
        {
            line += $" {exception.GetType().Name}: {exception.Message}";
        }

        lock (_gate)
        {
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }
}
