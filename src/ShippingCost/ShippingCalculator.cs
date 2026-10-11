namespace ShippingCost;

public static class ShippingCalculator
{
    /// <summary>
    /// Returns the shipping cost for items measured in meters.
    /// Pricing rules are not implemented yet; this returns a fixed quote.
    /// </summary>
    public static ShippingQuote GetShippingCost(IReadOnlyList<ShippingItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new ShippingQuote(
            Lines: [new CostLine(Type: "standard", Cost: 10.00m)],
            Total: 10.00m);
    }
}
