using System.Text.Json;

namespace StarCitizenMiningAssistant.Core.Localization;

public static class MineralShortcuts
{
    public static IReadOnlyDictionary<string, string> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            var key = entry.GetProperty("key").GetString();
            var value = entry.GetProperty("value").GetString();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            names[key] = value;
        }

        return names;
    }

    /// <summary>
    /// Replaces the value of an exact key. A longer key, such as a description, stays.
    /// A key that is not in the file is appended.
    /// </summary>
    public static string Apply(string text, IReadOnlyDictionary<string, string> names)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(names);
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var split = line.IndexOf('=');
            if (split <= 0)
            {
                continue;
            }

            var key = line[..split];
            if (!names.TryGetValue(key, out var value))
            {
                continue;
            }

            lines[i] = key + "=" + value;
            seen.Add(key);
        }

        var body = string.Join(newline, lines);
        var missing = new List<string>();
        foreach (var pair in names)
        {
            if (!seen.Contains(pair.Key))
            {
                missing.Add(pair.Key + "=" + pair.Value);
            }
        }

        if (missing.Count == 0)
        {
            return body;
        }

        if (body.Length > 0 && !body.EndsWith(newline, StringComparison.Ordinal))
        {
            body += newline;
        }

        return body + string.Join(newline, missing) + newline;
    }
}
