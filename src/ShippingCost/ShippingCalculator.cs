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

    private const decimal HeavyCost = 50m;
    private const decimal HeavyWeightLimitKilograms = 50m;
    private const decimal HeavyOverweightRatePerKilogram = 1m;

    /// <summary>
    /// Returns a shipping quote for items measured in meters and weighed in kilograms.
    /// Each item is one line, priced by its longest side:
    /// small (all sides under 10 cm) $3, medium (under 50 cm) $8,
    /// large (under 100 cm) $15, XL (any side 100 cm or more) $25.
    /// Weight above a size allowance is rounded up to the next whole kilogram
    /// and then charged at $2 per kilogram. Allowances are small 1 kg, medium 3 kg,
    /// large 6 kg, and XL 10 kg.
    /// A heavy parcel is $50 up to 50 kg and then $1 per whole kilogram over 50 kg.
    /// Each parcel uses the heavy price when it is lower than the size-based price.
    /// The line description lists the item's stored length, width, and height in centimeters
    /// and its stored weight in kilograms, and notes the billed whole kilograms
    /// over the allowance of the chosen price.
    /// Speedy shipping adds one more line equal to the sum of the item lines,
    /// which doubles the total.
    /// The returned lines are a fixed copy. The list cannot contain null items.
    /// </summary>
    public static ShippingQuote GetShippingCost(IReadOnlyList<ShippingItem> items, bool isSpeedyShipping = false)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (var index = 0; index < items.Count; index++)
        {
            if (items[index] is null)
                throw new ArgumentException($"Item at index {index} is null.", nameof(items));
        }

        var lines = new List<CostLine>(items.Count + (isSpeedyShipping ? 1 : 0));
        decimal itemsCost = 0m;
        foreach (var item in items)
        {
            var line = PriceItem(item);
            lines.Add(line);
            itemsCost = AddCost(itemsCost, line.Cost);
        }

        if (isSpeedyShipping)
        {
            lines.Add(new CostLine(CostLine.Speedy, itemsCost, "Speedy shipping"));
            itemsCost = AddCost(itemsCost, itemsCost);
        }

        return new ShippingQuote(lines, itemsCost);
    }

    private static decimal AddCost(decimal total, decimal cost)
    {
        try
        {
            return checked(total + cost);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException("items", "The shipping total is too large.");
        }
    }

    private static CostLine PriceItem(ShippingItem item)
    {
        var standard = StandardPrice(item);
        var heavy = HeavyPrice(item);
        return heavy.Cost < standard.Cost ? heavy : standard;
    }

    private static CostLine StandardPrice(ShippingItem item)
    {
        var longestSide = Math.Max(item.LengthMeters, Math.Max(item.WidthMeters, item.HeightMeters));
        var band = BandFor(longestSide);
        var billedOverweightKilograms = BilledOverweightKilograms(item.WeightKilograms, band.WeightLimitKilograms);
        var cost = band.Cost + billedOverweightKilograms * OverweightRatePerKilogram;
        return new CostLine(
            band.Type,
            cost,
            Describe(
                item.LengthMeters,
                item.WidthMeters,
                item.HeightMeters,
                item.WeightKilograms,
                billedOverweightKilograms));
    }

    private static CostLine HeavyPrice(ShippingItem item)
    {
        var billedOverweightKilograms = BilledOverweightKilograms(item.WeightKilograms, HeavyWeightLimitKilograms);
        var cost = HeavyCost + billedOverweightKilograms * HeavyOverweightRatePerKilogram;
        return new CostLine(
            CostLine.Heavy,
            cost,
            Describe(
                item.LengthMeters,
                item.WidthMeters,
                item.HeightMeters,
                item.WeightKilograms,
                billedOverweightKilograms));
    }

    private static ParcelBand BandFor(decimal longestSide)
    {
        if (longestSide < SmallLimitMeters)
            return new ParcelBand(CostLine.Small, SmallCost, SmallWeightLimitKilograms);
        if (longestSide < MediumLimitMeters)
            return new ParcelBand(CostLine.Medium, MediumCost, MediumWeightLimitKilograms);
        if (longestSide < LargeLimitMeters)
            return new ParcelBand(CostLine.Large, LargeCost, LargeWeightLimitKilograms);

        return new ParcelBand(CostLine.Xl, XlCost, XlWeightLimitKilograms);
    }

    /// <summary>
    /// Allowances are whole kilograms, so ceiling the weight and subtracting the allowance
    /// stays on a whole kilogram.
    /// </summary>
    private static decimal BilledOverweightKilograms(decimal weightKilograms, decimal limitKilograms)
    {
        if (weightKilograms <= limitKilograms)
            return 0m;

        return decimal.Ceiling(weightKilograms) - limitKilograms;
    }

    private readonly record struct ParcelBand(string Type, decimal Cost, decimal WeightLimitKilograms);

    private static string Describe(
        decimal lengthMeters,
        decimal widthMeters,
        decimal heightMeters,
        decimal weightKilograms,
        decimal excessKilograms)
    {
        var dimensions = string.Join(
            " × ",
            FormatCentimeters(lengthMeters),
            FormatCentimeters(widthMeters),
            FormatCentimeters(heightMeters));
        var description = $"{dimensions}, {FormatKilograms(weightKilograms)}";

        if (excessKilograms <= 0)
            return description;

        return $"{description}, {FormatKilograms(excessKilograms)} overweight";
    }

    private static string FormatCentimeters(decimal meters) =>
        FormattableString.Invariant($"{meters * 100m:0} cm");

    private static string FormatKilograms(decimal kilograms) =>
        FormattableString.Invariant($"{kilograms:0.###} kg");
}
