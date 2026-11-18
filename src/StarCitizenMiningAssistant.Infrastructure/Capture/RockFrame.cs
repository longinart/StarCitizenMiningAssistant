using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Infrastructure.Capture;

/// <summary>
/// Reads mass, resistance, and instability from one captured scan-results patch.
/// </summary>
public static class RockFrame
{
    public static RockOcrFields Read(byte[] bmp, IReadOnlyList<RockPanel.Word> words)
    {
        ArgumentNullException.ThrowIfNull(bmp);
        ArgumentNullException.ThrowIfNull(words);
        if (!TryGreen(bmp, out var green, out var width, out var height))
        {
            return Empty();
        }

        return RockPanel.Read(words, RockDigitReader.Read(width, height, green));
    }

    public static IReadOnlyList<ObservedValue<string>>? Lines(RockOcrFields fields)
    {
        if (!fields.HasPanel)
        {
            return null;
        }

        var lines = new List<ObservedValue<string>>();
        Add(lines, "MASS", fields.Mass);
        Add(lines, "RESISTANCE", fields.Resistance);
        Add(lines, "INSTABILITY", fields.Instability);
        return lines;
    }

    private static void Add(List<ObservedValue<string>> lines, string label, ObservedValue<double> value)
    {
        if (value.Confidence == RecognitionConfidence.Missing)
        {
            return;
        }

        lines.Add(ObservedValue<string>.Uncertain(
            label + " " + value.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static RockOcrFields Empty() =>
        new()
        {
            Mass = ObservedValue<double>.Missing(),
            Resistance = ObservedValue<double>.Missing(),
            Instability = ObservedValue<double>.Missing()
        };

    private static bool TryGreen(byte[] image, out byte[] green, out int width, out int height)
    {
        green = [];
        width = 0;
        height = 0;
        if (image.Length < 54 || image[0] != (byte)'B' || image[1] != (byte)'M')
        {
            return false;
        }

        var pixelOffset = BitConverter.ToInt32(image, 10);
        width = BitConverter.ToInt32(image, 18);
        height = BitConverter.ToInt32(image, 22);
        if (width < 8 || height < 8 || BitConverter.ToInt16(image, 28) != 24 || pixelOffset < 54)
        {
            return false;
        }

        var stride = ((width * 3 + 3) / 4) * 4;
        if (pixelOffset + (stride * height) > image.Length)
        {
            return false;
        }

        green = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var fileRow = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                green[(y * width) + x] = image[pixelOffset + (fileRow * stride) + (x * 3) + 1];
            }
        }

        return true;
    }
}
