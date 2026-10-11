using System.Collections.ObjectModel;

namespace ShippingCost;

public sealed record CostLine(string Type, decimal Cost, string Description)
{
    public const string Small = "small";
    public const string Medium = "medium";
    public const string Large = "large";
    public const string Xl = "xl";
    public const string Heavy = "heavy";
    public const string SmallMania = "small mania";
    public const string MediumMania = "medium mania";
    public const string MixedMania = "mixed mania";
    public const string Speedy = "speedy";
}

public sealed record ShippingQuote
{
    public ShippingQuote(IReadOnlyList<CostLine> lines, decimal total)
    {
        ArgumentNullException.ThrowIfNull(lines);
        Lines = new ReadOnlyCollection<CostLine>(lines.ToArray());
        Total = total;
    }

    public IReadOnlyList<CostLine> Lines { get; }

    public decimal Total { get; }

    public void Deconstruct(out IReadOnlyList<CostLine> lines, out decimal total)
    {
        lines = Lines;
        total = Total;
    }
}
