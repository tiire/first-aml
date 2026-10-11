namespace ShippingCost;

public sealed record ShippingItem(
    decimal LengthMeters,
    decimal WidthMeters,
    decimal HeightMeters,
    decimal WeightKilograms);
