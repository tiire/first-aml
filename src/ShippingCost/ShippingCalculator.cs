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

    private const decimal SmallWeightLimitKilograms = 1m;
    private const decimal MediumWeightLimitKilograms = 3m;
    private const decimal LargeWeightLimitKilograms = 6m;
    private const decimal XlWeightLimitKilograms = 10m;

    private const decimal OverweightRatePerKilogram = 2m;

    /// <summary>
    /// Returns a shipping quote for items measured in meters and weighed in kilograms.
    /// Each item is one line, priced by its longest side:
    /// small (all sides under 10 cm) $3, medium (under 50 cm) $8,
    /// large (under 100 cm) $15, XL (any side 100 cm or more) $25.
    /// Weight above the size limit adds $2 for each kilogram over:
    /// small 1 kg, medium 3 kg, large 6 kg, XL 10 kg.
    /// The line description lists that item's length, width, and height,
    /// and notes how many kilograms it is over the size limit.
    /// Speedy shipping adds one more line equal to the sum of the item lines,
    /// which doubles the total.
    /// </summary>
    public static ShippingQuote GetShippingCost(IReadOnlyList<ShippingItem> items, bool isSpeedyShipping = false)
    {
        ArgumentNullException.ThrowIfNull(items);

        var lines = items.Select(PriceItem).ToList();
        var itemsCost = lines.Sum(line => line.Cost);

        if (isSpeedyShipping)
            lines.Add(new CostLine("speedy", itemsCost, "Speedy shipping"));

        return new ShippingQuote(lines, lines.Sum(line => line.Cost));
    }

    private static CostLine PriceItem(ShippingItem item)
    {
        var longestSide = Math.Max(item.LengthMeters, Math.Max(item.WidthMeters, item.HeightMeters));

        var band = BandFor(longestSide);
        var excessKilograms = item.WeightKilograms - band.WeightLimitKilograms;
        var surcharge = excessKilograms > 0 ? excessKilograms * OverweightRatePerKilogram : 0m;
        return new CostLine(band.Type, band.Cost + surcharge, Describe(item, excessKilograms));
    }

    private static ParcelBand BandFor(decimal longestSide)
    {
        if (longestSide < SmallLimitMeters)
            return new ParcelBand("small", SmallCost, SmallWeightLimitKilograms);
        if (longestSide < MediumLimitMeters)
            return new ParcelBand("medium", MediumCost, MediumWeightLimitKilograms);
        if (longestSide < LargeLimitMeters)
            return new ParcelBand("large", LargeCost, LargeWeightLimitKilograms);

        return new ParcelBand("xl", XlCost, XlWeightLimitKilograms);
    }

    private readonly record struct ParcelBand(string Type, decimal Cost, decimal WeightLimitKilograms);

    private static string Describe(ShippingItem item, decimal excessKilograms)
    {
        var dimensions = string.Join(
            " × ",
            FormatMeters(item.LengthMeters),
            FormatMeters(item.WidthMeters),
            FormatMeters(item.HeightMeters));

        if (excessKilograms <= 0)
            return dimensions;

        return $"{dimensions}, {FormatKilograms(excessKilograms)} overweight";
    }

    private static string FormatMeters(decimal meters) =>
        FormattableString.Invariant($"{meters:0.#############################} m");

    private static string FormatKilograms(decimal kilograms) =>
        FormattableString.Invariant($"{kilograms:0.#############################} kg");
}
