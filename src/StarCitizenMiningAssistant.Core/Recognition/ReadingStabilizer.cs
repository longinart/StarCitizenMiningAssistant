namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Accepts a value only after the same value arrives on consecutive frames.
/// A different value starts the count again.
/// </summary>
public sealed class ReadingStabilizer<T>
{
    private readonly int _required;
    private readonly IEqualityComparer<T> _comparer;
    private T? _last;
    private int _count;
    private bool _hasLast;

    public ReadingStabilizer(int required = 5, IEqualityComparer<T>? comparer = null)
    {
        if (required < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(required));
        }

        _required = required;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public int Required => _required;

    public void Reset()
    {
        _hasLast = false;
        _count = 0;
        _last = default;
    }

    public bool Observe(T sample)
    {
        if (_hasLast && _comparer.Equals(_last!, sample))
        {
            if (_count < _required)
            {
                _count++;
            }
        }
        else
        {
            _last = sample;
            _hasLast = true;
            _count = 1;
        }

        return _count >= _required;
    }
}
