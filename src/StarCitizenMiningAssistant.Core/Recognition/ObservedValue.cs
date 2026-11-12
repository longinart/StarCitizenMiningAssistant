namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// A value obtained from recognition or manual entry, together with how much it can be trusted.
/// A numeric default such as zero is not the same as a missing reading.
/// </summary>
public readonly record struct ObservedValue<T>
{
    private ObservedValue(T? value, RecognitionConfidence confidence)
    {
        Value = value;
        Confidence = confidence;
    }

    public T? Value { get; }

    public RecognitionConfidence Confidence { get; }

    /// <summary>
    /// True only when the value was explicitly confirmed and is present.
    /// </summary>
    public bool IsConfirmed =>
        Confidence == RecognitionConfidence.Confirmed && Value is not null;

    public static ObservedValue<T> Missing() =>
        new(default, RecognitionConfidence.Missing);

    public static ObservedValue<T> Uncertain(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ObservedValue<T>(value, RecognitionConfidence.Uncertain);
    }

    public static ObservedValue<T> Confirmed(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ObservedValue<T>(value, RecognitionConfidence.Confirmed);
    }
}
