namespace StarCitizenMiningAssistant.Core.Recognition;

public readonly record struct StableStep(ObservedValue<double> Shown, ObservedValue<double>? LastSample);

/// <summary>
/// Confirms a labelled rock field after two identical samples.
/// Decision 015. One sample stays Uncertain. A missing sample keeps both
/// the shown value and the sample it will be compared with.
/// </summary>
public static class StableReading
{
    public static StableStep Next(ObservedValue<double> shown, ObservedValue<double>? lastSample, ObservedValue<double> sample)
    {
        if (sample.Confidence == RecognitionConfidence.Missing)
        {
            return new StableStep(shown, lastSample);
        }

        if (Agrees(lastSample, sample.Value) || (lastSample is null && shown.IsConfirmed && Agrees(shown, sample.Value)))
        {
            var confirmed = shown.IsConfirmed && Agrees(shown, sample.Value)
                ? shown
                : ObservedValue<double>.Confirmed(sample.Value);
            return new StableStep(confirmed, sample);
        }

        return new StableStep(ObservedValue<double>.Uncertain(sample.Value), sample);
    }

    private static bool Agrees(ObservedValue<double>? reading, double value) =>
        reading is ObservedValue<double> previous
        && previous.Confidence != RecognitionConfidence.Missing
        && Math.Abs(previous.Value - value) < 0.001;
}
