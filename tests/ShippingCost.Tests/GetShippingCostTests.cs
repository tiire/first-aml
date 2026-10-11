namespace ShippingCost.Tests;

public class GetShippingCostTests
{
    [Fact]
    public void ReturnsHardcodedQuote()
    {
        IReadOnlyList<ShippingItem> items =
        [
            new(LengthMeters: 1.2m, WidthMeters: 0.8m, HeightMeters: 0.4m),
            new(LengthMeters: 0.3m, WidthMeters: 0.3m, HeightMeters: 0.3m),
        ];

        var quote = ShippingCalculator.GetShippingCost(items);

        var line = Assert.Single(quote.Lines);
        Assert.Equal("standard", line.Type);
        Assert.Equal(10.00m, line.Cost);
        Assert.Equal(10.00m, quote.Total);
    }

    [Fact]
    public void ReturnsHardcodedQuoteForAnEmptyList()
    {
        var quote = ShippingCalculator.GetShippingCost([]);

        Assert.Equal(10.00m, quote.Total);
        Assert.Single(quote.Lines);
    }

    [Fact]
    public void RejectsNullItems()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ShippingCalculator.GetShippingCost(null!));

        Assert.Equal("items", exception.ParamName);
    }
}
