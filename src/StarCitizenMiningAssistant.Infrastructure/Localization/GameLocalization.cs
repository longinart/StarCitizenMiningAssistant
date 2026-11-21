using System.Text;
using StarCitizenMiningAssistant.Core.Localization;
using ZstdSharp;

namespace StarCitizenMiningAssistant.Infrastructure.Localization;

public sealed record GameChannel(string Name, string Folder);

public sealed record ChannelResult(string Name, string Folder, bool Applied, string Detail);

public static class GameLocalization
{
    private const string EnglishIni = "Data\\Localization\\english\\global.ini";

    public static IReadOnlyList<GameChannel> Find()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            var path = Path.Combine(drive.RootDirectory.FullName, "Program Files", "Roberts Space Industries", "StarCitizen");
            if (Directory.Exists(path))
            {
                roots.Add(path);
            }
        }

        var channels = new List<GameChannel>();
        foreach (var root in roots)
        {
            foreach (var folder in Directory.GetDirectories(root))
            {
                if (File.Exists(Path.Combine(folder, "Data.p4k")))
                {
                    channels.Add(new GameChannel(Path.GetFileName(folder), folder));
                }
            }
        }

        return channels
            .OrderBy(channel => Rank(channel.Name))
            .ThenBy(channel => channel.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<ChannelResult> Apply(IReadOnlyDictionary<string, string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var results = new List<ChannelResult>();
        foreach (var channel in Find())
        {
            results.Add(ApplyChannel(channel, names));
        }

        return results;
    }

    private static ChannelResult ApplyChannel(GameChannel channel, IReadOnlyDictionary<string, string> names)
    {
        var loose = Path.Combine(channel.Folder, "data", "Localization", "english", "global.ini");
        string text;
        try
        {
            text = File.Exists(loose) ? File.ReadAllText(loose) : ReadPacked(channel.Folder);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return new ChannelResult(channel.Name, channel.Folder, false, ex.Message);
        }

        if (!text.Contains("items_commodities_tin_ore=", StringComparison.Ordinal))
        {
            return new ChannelResult(channel.Name, channel.Folder, false, "The english localization does not contain the ore names.");
        }

        var patched = MineralShortcuts.Apply(text, names);
        if (patched == text && File.Exists(loose))
        {
            EnsureLanguage(channel.Folder);
            return new ChannelResult(channel.Name, channel.Folder, true, "Already using these short names.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(loose)!);
        string? backup = null;
        if (File.Exists(loose))
        {
            backup = loose + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(loose, backup, overwrite: false);
        }

        var temporary = loose + ".writing";
        File.WriteAllText(temporary, patched, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        File.Move(temporary, loose, overwrite: true);
        EnsureLanguage(channel.Folder);
        var detail = backup is null
            ? "Wrote the english localization with the short ore names."
            : "Updated the english localization. Backup: " + Path.GetFileName(backup);
        return new ChannelResult(channel.Name, channel.Folder, true, detail);
    }

    private static void EnsureLanguage(string folder)
    {
        var path = Path.Combine(folder, "user.cfg");
        var text = File.Exists(path) ? File.ReadAllText(path) : "";
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("g_language", StringComparison.OrdinalIgnoreCase)
                && trimmed.Contains('=', StringComparison.Ordinal))
            {
                return;
            }
        }

        if (text.Length > 0 && !text.EndsWith('\n'))
        {
            text += "\r\n";
        }

        File.WriteAllText(path, text + "g_language = english\r\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ReadPacked(string folder)
    {
        var archive = Path.Combine(folder, "Data.p4k");
        if (!File.Exists(archive))
        {
            throw new FileNotFoundException("Data.p4k was not found.", archive);
        }

        using var stream = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var entry = FindEntry(stream, EnglishIni);
        stream.Seek(entry.Offset, SeekOrigin.Begin);
        Span<byte> local = stackalloc byte[30];
        stream.ReadExactly(local);
        if (local[0] != (byte)'P' || local[1] != (byte)'K')
        {
            throw new InvalidDataException("The localization entry is not a zip local header.");
        }

        var nameLength = BitConverter.ToUInt16(local[26..]);
        var extraLength = BitConverter.ToUInt16(local[28..]);
        stream.Seek(nameLength + extraLength, SeekOrigin.Current);
        var compressed = new byte[entry.CompressedSize];
        stream.ReadExactly(compressed);
        byte[] raw = entry.Method switch
        {
            0 => compressed,
            100 => Decompress(compressed, entry.UncompressedSize),
            _ => throw new InvalidDataException("The localization entry uses compression " + entry.Method + ".")
        };
        return Decode(raw);
    }

    private static byte[] Decompress(byte[] compressed, int size)
    {
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(compressed, size).ToArray();
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        if (bytes.Length > 8 && bytes[1] == 0 && bytes[3] == 0)
        {
            return Encoding.Unicode.GetString(bytes);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private readonly record struct PackedEntry(long Offset, int CompressedSize, int UncompressedSize, ushort Method);

    private static PackedEntry FindEntry(Stream stream, string name)
    {
        var needle = Encoding.ASCII.GetBytes(name);
        stream.Seek(0, SeekOrigin.End);
        var length = stream.Length;
        var tailCount = (int)Math.Min(length, 2_000_000);
        var tail = new byte[tailCount];
        stream.Seek(length - tailCount, SeekOrigin.Begin);
        stream.ReadExactly(tail);
        var locator = tail.AsSpan().LastIndexOf("PK\u0006\u0007"u8);
        if (locator < 0 || locator + 16 > tail.Length)
        {
            throw new InvalidDataException("Data.p4k has no zip64 directory.");
        }

        var eocd = BitConverter.ToInt64(tail, locator + 8);
        stream.Seek(eocd, SeekOrigin.Begin);
        var header = new byte[56];
        stream.ReadExactly(header);
        var directorySize = BitConverter.ToInt64(header, 40);
        var directoryOffset = BitConverter.ToInt64(header, 48);
        stream.Seek(directoryOffset, SeekOrigin.Begin);
        var pending = directorySize;
        var carry = Array.Empty<byte>();
        while (pending > 0)
        {
            var take = (int)Math.Min(pending, 8_000_000);
            var block = new byte[take];
            stream.ReadExactly(block);
            pending -= take;
            var blob = new byte[carry.Length + block.Length];
            carry.CopyTo(blob, 0);
            block.CopyTo(blob, carry.Length);
            var search = 0;
            while (search < blob.Length)
            {
                var found = blob.AsSpan(search).IndexOf(needle);
                if (found < 0)
                {
                    break;
                }

                var index = search + found;
                search = index + needle.Length;
                if (index < 46)
                {
                    continue;
                }

                var entry = blob.AsSpan(index - 46);
                if (entry.Length < 46 || entry[0] != (byte)'P' || entry[1] != (byte)'K' || entry[2] != 1 || entry[3] != 2)
                {
                    continue;
                }

                var nameLength = BitConverter.ToUInt16(entry[28..]);
                var extraLength = BitConverter.ToUInt16(entry[30..]);
                if (nameLength != needle.Length || 46 + nameLength + extraLength > entry.Length)
                {
                    continue;
                }

                return ReadEntry(entry, needle.Length);
            }

            carry = blob[^Math.Min(blob.Length, 800)..];
        }

        throw new InvalidDataException("english global.ini was not found in Data.p4k.");
    }

    private static PackedEntry ReadEntry(ReadOnlySpan<byte> header, int nameLength)
    {
        var method = BitConverter.ToUInt16(header[10..]);
        var compressedField = BitConverter.ToUInt32(header[20..]);
        var uncompressedField = BitConverter.ToUInt32(header[24..]);
        var extraLength = BitConverter.ToUInt16(header[30..]);
        var offsetField = BitConverter.ToUInt32(header[42..]);
        var extra = header.Slice(46 + nameLength, extraLength);
        long compressed = compressedField;
        long uncompressed = uncompressedField;
        long offset = offsetField;
        var cursor = 0;
        while (cursor + 4 <= extra.Length)
        {
            var id = BitConverter.ToUInt16(extra[cursor..]);
            var size = BitConverter.ToUInt16(extra[(cursor + 2)..]);
            if (size > extra.Length - (cursor + 4))
            {
                break;
            }

            var body = extra.Slice(cursor + 4, size);
            cursor += 4 + size;
            if (id != 1)
            {
                continue;
            }

            var used = 0;
            if (uncompressedField == uint.MaxValue)
            {
                uncompressed = BitConverter.ToInt64(body[used..]);
                used += 8;
            }

            if (compressedField == uint.MaxValue)
            {
                compressed = BitConverter.ToInt64(body[used..]);
                used += 8;
            }

            if (offsetField == uint.MaxValue && used + 8 <= body.Length)
            {
                offset = BitConverter.ToInt64(body[used..]);
            }

            break;
        }

        if (compressed is <= 0 or > int.MaxValue || uncompressed is <= 0 or > int.MaxValue)
        {
            throw new InvalidDataException("The localization entry has an unexpected size.");
        }

        return new PackedEntry(offset, (int)compressed, (int)uncompressed, method);
    }

    private static int Rank(string name) => name.ToUpperInvariant() switch
    {
        "LIVE" => 0,
        "PTU" => 1,
        "EPTU" => 2,
        "HOTFIX" => 3,
        _ => 4
    };
}
