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
    /// Each parcel uses the heavy price when it is lower than the size-based price,
    /// unless keeping the size-based price lets a discount make the whole quote cheaper.
    /// The line description lists the item's stored length, width, and height in centimeters
    /// and its stored weight in kilograms, and notes the billed whole kilograms
    /// over the allowance of the chosen price.
    /// Small parcel mania makes every 4th small parcel free, medium parcel mania every
    /// 3rd medium parcel, and mixed parcel mania every 5th parcel. A parcel is used in
    /// at most one discount, and the combination that saves the most is applied.
    /// Within a discount, the cheapest parcel in each completed group is free.
    /// Each discount is a separate line with a negative cost.
    /// Speedy shipping adds one more line equal to the sum of the item and discount lines,
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

        var lines = PriceWithDiscounts(items);

        decimal itemsCost = 0m;
        foreach (var line in lines)
            itemsCost = AddCost(itemsCost, line.Cost);

        if (isSpeedyShipping)
        {
            lines.Add(new CostLine(CostLine.Speedy, itemsCost, "Speedy shipping"));
            itemsCost = AddCost(itemsCost, itemsCost);
        }

        return new ShippingQuote(lines, itemsCost);
    }

    private const int SmallGroupSize = 4;
    private const int MediumGroupSize = 3;
    private const int MixedGroupSize = 5;

    /// <summary>
    /// A parcel normally takes the cheaper of its standard and heavy prices.
    /// It takes its standard price only when that lets it join small or medium mania,
    /// because a higher price never lowers the total outside those discounts.
    /// Within a size band both prices rise with weight, so ordering by the cheaper
    /// price and then by weight also orders each mania group by standard price,
    /// and the parcel that completes a group is always that group's cheapest.
    /// </summary>
    private static List<CostLine> PriceWithDiscounts(IReadOnlyList<ShippingItem> items)
    {
        var count = items.Count;
        var standardLines = new CostLine[count];
        var cheapestLines = new CostLine[count];
        for (var index = 0; index < count; index++)
        {
            standardLines[index] = StandardPrice(items[index]);
            var heavy = HeavyPrice(items[index]);
            cheapestLines[index] = heavy.Cost < standardLines[index].Cost ? heavy : standardLines[index];
        }

        var lines = new List<CostLine>(count + 3);
        if (count == 0)
            return lines;

        var order = Enumerable.Range(0, count)
            .OrderByDescending(index => cheapestLines[index].Cost)
            .ThenByDescending(index => items[index].WeightKilograms)
            .ThenBy(index => index)
            .ToArray();

        var savings = new decimal[count + 1, SmallGroupSize, MediumGroupSize, MixedGroupSize];
        var reachable = new bool[count + 1, SmallGroupSize, MediumGroupSize, MixedGroupSize];
        var choice = new byte[count + 1, SmallGroupSize, MediumGroupSize, MixedGroupSize];
        var previousSmall = new int[count + 1, SmallGroupSize, MediumGroupSize, MixedGroupSize];
        var previousMedium = new int[count + 1, SmallGroupSize, MediumGroupSize, MixedGroupSize];
        var previousMixed = new int[count + 1, SmallGroupSize, MediumGroupSize, MixedGroupSize];
        reachable[0, 0, 0, 0] = true;

        for (var index = 0; index < count; index++)
        {
            var standard = standardLines[order[index]];
            var cheapest = cheapestLines[order[index]];
            for (var small = 0; small < SmallGroupSize; small++)
            for (var medium = 0; medium < MediumGroupSize; medium++)
            for (var mixed = 0; mixed < MixedGroupSize; mixed++)
            {
                if (!reachable[index, small, medium, mixed])
                    continue;

                var saved = savings[index, small, medium, mixed];
                Consider(index, small, medium, mixed, saved, 0, small, medium, mixed);
                var extra = standard.Cost - cheapest.Cost;
                if (standard.Type == CostLine.Small)
                {
                    var nextSmall = (small + 1) % SmallGroupSize;
                    var added = nextSmall == 0 ? standard.Cost : 0m;
                    Consider(index, small, medium, mixed, saved + added - extra, 1, nextSmall, medium, mixed);
                }

                if (standard.Type == CostLine.Medium)
                {
                    var nextMedium = (medium + 1) % MediumGroupSize;
                    var added = nextMedium == 0 ? standard.Cost : 0m;
                    Consider(index, small, medium, mixed, saved + added - extra, 2, small, nextMedium, mixed);
                }

                var nextMixed = (mixed + 1) % MixedGroupSize;
                var mixedAdded = nextMixed == 0 ? cheapest.Cost : 0m;
                Consider(index, small, medium, mixed, saved + mixedAdded, 3, small, medium, nextMixed);
            }
        }

        var bestSmall = 0;
        var bestMedium = 0;
        var bestMixed = 0;
        var bestSavings = 0m;
        for (var small = 0; small < SmallGroupSize; small++)
        for (var medium = 0; medium < MediumGroupSize; medium++)
        for (var mixed = 0; mixed < MixedGroupSize; mixed++)
        {
            if (!reachable[count, small, medium, mixed] || savings[count, small, medium, mixed] <= bestSavings)
                continue;

            bestSavings = savings[count, small, medium, mixed];
            bestSmall = small;
            bestMedium = medium;
            bestMixed = mixed;
        }

        var chosenLines = new CostLine[count];
        var discounts = new List<(int ParcelIndex, CostLine Line)>();
        for (var index = count; index > 0; index--)
        {
            var taken = choice[index, bestSmall, bestMedium, bestMixed];
            var parcelIndex = order[index - 1];
            chosenLines[parcelIndex] = taken is 1 or 2 ? standardLines[parcelIndex] : cheapestLines[parcelIndex];
            if ((taken == 1 && bestSmall == 0) || (taken == 2 && bestMedium == 0) || (taken == 3 && bestMixed == 0))
                discounts.Add((parcelIndex, DiscountLine(taken, chosenLines[parcelIndex].Cost)));

            var previousSmallCount = previousSmall[index, bestSmall, bestMedium, bestMixed];
            var previousMediumCount = previousMedium[index, bestSmall, bestMedium, bestMixed];
            var previousMixedCount = previousMixed[index, bestSmall, bestMedium, bestMixed];
            bestSmall = previousSmallCount;
            bestMedium = previousMediumCount;
            bestMixed = previousMixedCount;
        }

        lines.AddRange(chosenLines);
        foreach (var discount in discounts.OrderBy(discount => discount.ParcelIndex))
            lines.Add(discount.Line);

        return lines;

        void Consider(
            int index,
            int small,
            int medium,
            int mixed,
            decimal saved,
            byte choiceTaken,
            int nextSmall,
            int nextMedium,
            int nextMixed)
        {
            var next = index + 1;
            if (reachable[next, nextSmall, nextMedium, nextMixed] && savings[next, nextSmall, nextMedium, nextMixed] >= saved)
                return;

            reachable[next, nextSmall, nextMedium, nextMixed] = true;
            savings[next, nextSmall, nextMedium, nextMixed] = saved;
            choice[next, nextSmall, nextMedium, nextMixed] = choiceTaken;
            previousSmall[next, nextSmall, nextMedium, nextMixed] = small;
            previousMedium[next, nextSmall, nextMedium, nextMixed] = medium;
            previousMixed[next, nextSmall, nextMedium, nextMixed] = mixed;
        }
    }

    private static CostLine DiscountLine(byte discount, decimal parcelCost) => discount switch
    {
        1 => new CostLine(CostLine.SmallMania, -parcelCost, "Small parcel mania: every 4th small parcel is free"),
        2 => new CostLine(CostLine.MediumMania, -parcelCost, "Medium parcel mania: every 3rd medium parcel is free"),
        _ => new CostLine(CostLine.MixedMania, -parcelCost, "Mixed parcel mania: every 5th parcel is free"),
    };

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
