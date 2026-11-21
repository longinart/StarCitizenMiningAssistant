using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarCitizenMiningAssistant.Core.Recognition;
using StarCitizenMiningAssistant.Core.Refining;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class CompositionBoard : ViewModelBase
{
    private readonly Action _changed;
    private RefineryCatalog _catalog = RefineryCatalog.Empty;
    private string _totalScuText = "";
    private string _rawPrice = "Raw price: —";
    private string _refinedPrice = "Refined price: —";
    private string _bestTerminals = "";
    public const string DeleteRow = "DELETE this row";

    public CompositionBoard(IReadOnlyList<string> mineralNames, Action changed)
    {
        MineralNames = mineralNames;
        Choices = new[] { DeleteRow }.Concat(mineralNames).ToList();
        _changed = changed;
        Rows = [];
    }

    public IReadOnlyList<string> MineralNames { get; }

    public IReadOnlyList<string> Choices { get; }

    public bool CanRemove => Rows.Count > 0;

    public ObservableCollection<CompositionEntry> Rows { get; }

    public bool CanAdd => Rows.Count < 6;

    public string TotalScuText
    {
        get => _totalScuText;
        set
        {
            if (Set(ref _totalScuText, value))
            {
                Recalculate(_method, _home, _showRefined);
                _changed();
            }
        }
    }

    public string RawPrice => _rawPrice;

    public string RefinedPrice => _refinedPrice;

    public string BestTerminals => _bestTerminals;

    public bool HasBestTerminals => _bestTerminals.Length > 0;

    public bool IsCompositionValid =>
        CompositionShares.Fits(Rows.Select(row => Parse(row.PercentText) ?? 0));

    private string _method = "";
    private string _home = "";
    private bool _showRefined;

    public void UseCatalog(RefineryCatalog catalog) => _catalog = catalog ?? RefineryCatalog.Empty;

    public void AddRow()
    {
        if (!CanAdd)
        {
            return;
        }

        var row = new CompositionEntry(OnRowChanged, RemoveRow, Choices);
        Rows.Add(row);
        OnPropertyChanged(nameof(CanAdd));
        OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(IsCompositionValid));
        _changed();
    }

    public void RemoveLast()
    {
        if (Rows.Count == 0)
        {
            return;
        }

        Rows.RemoveAt(Rows.Count - 1);
        OnPropertyChanged(nameof(CanAdd));
        OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(IsCompositionValid));
        Recalculate(_method, _home, _showRefined);
        _changed();
    }

    private void RemoveRow(CompositionEntry row)
    {
        if (!Rows.Remove(row))
        {
            return;
        }

        OnPropertyChanged(nameof(CanAdd));
        OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(IsCompositionValid));
        Recalculate(_method, _home, _showRefined);
        _changed();
    }

    public void ApplyObserved(IReadOnlyList<ObservedShare> shares, ObservedValue<double>? totalScu = null)
    {
        if (totalScu is ObservedValue<double> total && total.Confidence != RecognitionConfidence.Missing)
        {
            var text = total.Value.ToString("0.##", CultureInfo.CurrentCulture);
            if (_totalScuText != text)
            {
                _totalScuText = text;
                OnPropertyChanged(nameof(TotalScuText));
            }
        }

        var incoming = shares
            .Where(share => share.Percent.Confidence != RecognitionConfidence.Missing)
            .Select(share => share with { Name = CompositionShares.DisplayName(share.Name, MineralNames) })
            .ToList();
        if (incoming.Count == 0)
        {
            Recalculate(_method, _home, _showRefined);
            return;
        }

        if (Rows.Count == incoming.Count && Rows.Zip(incoming).All(pair =>
                pair.First.Name.Equals(pair.Second.Name, StringComparison.Ordinal)
                && pair.First.PercentText == pair.Second.Percent.Value.ToString("0.##", CultureInfo.CurrentCulture)))
        {
            return;
        }

        Rows.Clear();
        foreach (var share in incoming)
        {
            Rows.Add(CompositionEntry.FromScan(share.Name, share.Percent, OnRowChanged, RemoveRow, Choices));
        }

        OnPropertyChanged(nameof(CanAdd));
        OnPropertyChanged(nameof(CanRemove));
        Recalculate(_method, _home, _showRefined);
    }

    public void Recalculate(string? method, string? home, bool showRefined)
    {
        _method = method ?? "";
        _home = home ?? "";
        _showRefined = showRefined;
        var total = Parse(_totalScuText);
        var rock = RefineryCalculator.Calculate(
            total ?? 0,
            _method,
            _home,
            Rows.Select(row => new RefineryRow(row.Name, Parse(row.PercentText) ?? 0, row.Confirmed)).ToList(),
            _catalog);
        foreach (var row in Rows)
        {
            var match = rock.Components.FirstOrDefault(component =>
                component.Name.Equals(row.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                && component.Percentage == (Parse(row.PercentText) ?? -1));
            var amount = match is null
                ? null
                : showRefined ? match.RefinedScu : match.RawScu;
            row.SetScu(amount is double scu ? scu.ToString("0.##", CultureInfo.CurrentCulture) : "");
        }

        OnPropertyChanged(nameof(IsCompositionValid));
        if (!IsCompositionValid)
        {
            SetPrice(ref _rawPrice, "Raw price: ", null, nameof(RawPrice));
            SetPrice(ref _refinedPrice, "Refined price: ", null, nameof(RefinedPrice));
            if (Set(ref _bestTerminals, ""))
            {
                OnPropertyChanged(nameof(HasBestTerminals));
            }

            return;
        }

        var prices = CommodityPricing.Quote(rock, _catalog);
        SetPrice(ref _rawPrice, "Raw price: ", prices.RawAuec, nameof(RawPrice));
        SetPrice(ref _refinedPrice, "Refined price: ", prices.RefinedAuec, nameof(RefinedPrice));
        var top = prices.TopRefined.Count > 0 ? prices.TopRefined : prices.TopRaw;
        var lines = string.Join(Environment.NewLine, top.Select(quote => quote.Terminal + " — " + quote.Auec.ToString("N0", CultureInfo.CurrentCulture) + " aUEC"));
        if (Set(ref _bestTerminals, lines))
        {
            OnPropertyChanged(nameof(HasBestTerminals));
        }
    }

    private void OnRowChanged()
    {
        Recalculate(_method, _home, _showRefined);
        _changed();
    }

    private void SetPrice(ref string field, string label, long? auec, string property)
    {
        var text = auec is long price
            ? label + price.ToString("N0", CultureInfo.CurrentCulture) + " aUEC"
            : label + "—";
        if (field != text)
        {
            field = text;
            OnPropertyChanged(property);
        }
    }

    private static double? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        return null;
    }
}

public sealed class CompositionEntry : ViewModelBase
{
    private readonly Action _changed;
    private readonly Action<CompositionEntry>? _delete;
    private string _name = "";
    private string _percentText = "";
    private string _scuText = "";
    private bool _confirmed;

    public IReadOnlyList<string> Options { get; }

    private CompositionEntry(Action changed, bool confirmed, Action<CompositionEntry>? delete, IReadOnlyList<string> options, string name)
    {
        _changed = changed;
        _confirmed = confirmed;
        _delete = delete;
        Options = options.Any(item => item.Equals(name, StringComparison.OrdinalIgnoreCase)) || name.Length == 0
            ? options
            : new[] { name }.Concat(options).ToList();
    }

    public static CompositionEntry FromScan(string name, ObservedValue<double> percent, Action changed, Action<CompositionEntry> delete, IReadOnlyList<string> options)
    {
        var row = new CompositionEntry(changed, percent.Confidence != RecognitionConfidence.Missing, delete, options, name)
        {
            _name = name,
            _percentText = percent.Confidence == RecognitionConfidence.Missing
                ? ""
                : percent.Value.ToString("0.##", CultureInfo.CurrentCulture)
        };
        return row;
    }

    public CompositionEntry(Action changed, Action<CompositionEntry> delete, IReadOnlyList<string> options)
        : this(changed, false, delete, options, "")
    {
    }

    public string Name
    {
        get => _name;
        set
        {
            if (string.Equals(value, CompositionBoard.DeleteRow, StringComparison.OrdinalIgnoreCase))
            {
                var row = this;
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(() => _delete?.Invoke(row));
                return;
            }

            if (Set(ref _name, value))
            {
                _confirmed = true;
                _changed();
            }
        }
    }

    public string PercentText
    {
        get => _percentText;
        set
        {
            if (Set(ref _percentText, value))
            {
                _confirmed = true;
                _changed();
            }
        }
    }

    public string ScuText => _scuText;

    public bool Confirmed => _confirmed;

    public void SetScu(string text)
    {
        if (_scuText != text)
        {
            _scuText = text;
            OnPropertyChanged(nameof(ScuText));
        }
    }
}
