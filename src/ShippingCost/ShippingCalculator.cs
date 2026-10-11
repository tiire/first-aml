namespace ShippingCost;

public static class ShippingCalculator
{
    private const decimal SmallLimitMeters = 0.10m;
    private const decimal MediumLimitMeters = 0.50m;
    private const decimal LargeLimitMeters = 1.00m;

    private const decimal SmallCost = 3m;
    private const decimal MediumCost = 8m;
    private const decimal LargeCost = 15m;
    private const decimal XlCost = 25m;

    /// <summary>
    /// Returns a shipping quote for items measured in meters.
    /// Each item is one line, priced by its longest side:
    /// small (all sides under 10 cm) $3, medium (under 50 cm) $8,
    /// large (under 100 cm) $15, XL (any side 100 cm or more) $25.
    /// The line description lists that item's length, width, and height.
    /// </summary>
    public static ShippingQuote GetShippingCost(IReadOnlyList<ShippingItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var lines = items.Select(PriceItem).ToArray();
        return new ShippingQuote(lines, lines.Sum(line => line.Cost));
    }

    private static CostLine PriceItem(ShippingItem item)
    {
        var longestSide = Math.Max(item.LengthMeters, Math.Max(item.WidthMeters, item.HeightMeters));

        var description = Describe(item);

        if (longestSide < SmallLimitMeters)
            return new CostLine("small", SmallCost, description);
        if (longestSide < MediumLimitMeters)
            return new CostLine("medium", MediumCost, description);
        if (longestSide < LargeLimitMeters)
            return new CostLine("large", LargeCost, description);

        return new CostLine("xl", XlCost, description);
    }

    private static string Describe(ShippingItem item) =>
        string.Join(
            " × ",
            FormatMeters(item.LengthMeters),
            FormatMeters(item.WidthMeters),
            FormatMeters(item.HeightMeters));

    private static string FormatMeters(decimal meters) =>
        FormattableString.Invariant($"{meters:0.#############################} m");
}
