namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Keeps the last signature or rock field when a capture has no number.
/// Decision 014. A sample that contains a number replaces the field.
/// </summary>
public readonly record struct HeldReading(ObservedValue<double> Sample, ObservedValue<double> Held);

public static class ReadingHold
{
    public static ObservedValue<double> Next(ObservedValue<double> previous, ObservedValue<double> sample) =>
        sample.Confidence == RecognitionConfidence.Missing ? previous : sample;
}
