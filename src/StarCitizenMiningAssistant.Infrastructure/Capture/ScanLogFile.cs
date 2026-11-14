namespace StarCitizenMiningAssistant.Infrastructure.Capture;

/// <summary>
/// Writes one automatic scan still and a matching line. The folder is
/// LocalAppData\StarCitizenMiningAssistant\scan-log.
/// </summary>
public static class ScanLogFile
{
    public static string Folder()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "StarCitizenMiningAssistant", "scan-log");
    }

    public static string? Save(string target, byte[] bmp, string reading, string confidence, string detail)
    {
        ArgumentNullException.ThrowIfNull(bmp);
        if (bmp.Length < 54 || (target != "signature" && target != "rock"))
        {
            return null;
        }

        try
        {
            var folder = Folder();
            Directory.CreateDirectory(folder);
            var stamp = DateTimeOffset.Now;
            var name = target + "-" + stamp.ToString("yyyyMMdd-HHmmss-fff") + ".bmp";
            File.WriteAllBytes(Path.Combine(folder, name), bmp);
            var log = Path.Combine(folder, "log.tsv");
            var header = !File.Exists(log);
            var line = string.Join(
                '\t',
                stamp.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                target,
                name,
                OneLine(reading),
                OneLine(confidence),
                OneLine(detail));
            var text = (header ? "time\ttarget\tfile\treading\tconfidence\tdetail" + Environment.NewLine : "")
                + line
                + Environment.NewLine;
            File.AppendAllText(log, text);
            return Path.Combine(folder, name);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string OneLine(string? text) =>
        (text ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
