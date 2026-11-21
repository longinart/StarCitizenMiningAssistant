namespace StarCitizenMiningAssistant.Core.Refining;

public sealed record MineralComponent(string Name, double Percentage, double RawScu, double? RefinedScu);

public sealed record RockComposition(double TotalScu, IReadOnlyList<MineralComponent> Components);
