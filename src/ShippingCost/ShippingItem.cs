namespace ShippingCost;

/// <summary>
/// A parcel measured in meters and kilograms.
/// The stored dimensions are rounded to the nearest centimeter and the stored weight
/// to the nearest gram. A midpoint rounds away from zero. A value that rounds to
/// 0 cm or 0 g is rejected.
/// </summary>
public sealed record ShippingItem
{
    /// <summary>Maximum length, width, or height, in meters.</summary>
    public const decimal MaxDimensionMeters = 4m;

    /// <summary>Maximum weight, in kilograms.</summary>
    public const decimal MaxWeightKilograms = 100m;

    public ShippingItem(decimal lengthMeters, decimal widthMeters, decimal heightMeters, decimal weightKilograms)
    {
        LengthMeters = lengthMeters;
        WidthMeters = widthMeters;
        HeightMeters = heightMeters;
        WeightKilograms = weightKilograms;
    }

    public decimal LengthMeters
    {
        get;
        init => field = NormalizeDimension(value, nameof(LengthMeters));
    }

    public decimal WidthMeters
    {
        get;
        init => field = NormalizeDimension(value, nameof(WidthMeters));
    }

    public decimal HeightMeters
    {
        get;
        init => field = NormalizeDimension(value, nameof(HeightMeters));
    }

    public decimal WeightKilograms
    {
        get;
        init => field = NormalizeWeight(value, nameof(WeightKilograms));
    }

    private static decimal NormalizeDimension(decimal value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxDimensionMeters, name);

        var centimeters = decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero);
        if (centimeters == 0m)
            throw new ArgumentOutOfRangeException(name, value, "Dimension rounds to 0 cm.");

        return centimeters / 100m;
    }

    private static decimal NormalizeWeight(decimal value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxWeightKilograms, name);

        var grams = decimal.Round(value * 1000m, 0, MidpointRounding.AwayFromZero);
        if (grams == 0m)
            throw new ArgumentOutOfRangeException(name, value, "Weight rounds to 0 g.");

        return grams / 1000m;
    }
}
