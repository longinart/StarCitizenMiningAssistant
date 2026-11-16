namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// A new signature replaces the held number only after the same value is seen
/// twice in a row. One odd frame does not change it. Decision 021.
/// </summary>
public sealed class RepeatedReading
{
    private double? _pending;

    public ObservedValue<double> Gate(ObservedValue<double> sample)
    {
        if (sample.Confidence == RecognitionConfidence.Missing)
        {
            _pending = null;
            return ObservedValue<double>.Missing();
        }

        if (_pending is double pending && Math.Abs(pending - sample.Value) < 0.001)
        {
            return sample;
        }

        _pending = sample.Value;
        return ObservedValue<double>.Missing();
    }
}
