namespace StarCitizenMiningAssistant.Core.Recognition;

public sealed record OcrFragment(string Text, double? Confidence);

/// <summary>
/// Lines read from one image. Text from an engine stays Uncertain until a
/// decision says a confidence score may confirm it. Decision 012 does not.
/// </summary>
public sealed class OcrDocument
{
    private OcrDocument(string? failure, string language, IReadOnlyList<ObservedValue<string>> lines)
    {
        Failure = failure;
        Language = language;
        Lines = lines;
    }

    public string? Failure { get; }

    public string Language { get; }

    public IReadOnlyList<ObservedValue<string>> Lines { get; }

    public static OcrDocument Failed(string reason) =>
        new(reason, "", []);

    public static OcrDocument FromEngine(string language, IReadOnlyList<OcrFragment> fragments)
    {
        var lines = new List<ObservedValue<string>>();
        foreach (var fragment in fragments)
        {
            var observed = OcrObservation.Text(fragment.Text, fragment.Confidence);
            if (observed.Confidence != RecognitionConfidence.Missing)
            {
                lines.Add(observed);
            }
        }

        return new OcrDocument(null, language, lines);
    }
}

public static class OcrObservation
{
    public static ObservedValue<string> Text(string? text, double? confidence)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ObservedValue<string>.Missing();
        }

        // A confidence score is not a confirmation. See decision 012.
        _ = confidence;
        return ObservedValue<string>.Uncertain(text.Trim());
    }
}
