namespace ShippingCost.Tests;

public class GetShippingCostTests
{
    [Theory]
    [InlineData(0.09, 0.09, 0.09, "small", 3)]
    [InlineData(0.01, 0.02, 0.099, "small", 3)]
    [InlineData(0.10, 0.01, 0.01, "medium", 8)]
    [InlineData(0.49, 0.20, 0.10, "medium", 8)]
    [InlineData(0.50, 0.10, 0.10, "large", 15)]
    [InlineData(0.99, 0.40, 0.20, "large", 15)]
    [InlineData(1.00, 0.01, 0.01, "xl", 25)]
    [InlineData(2.40, 0.30, 0.05, "xl", 25)]
    public void PricesOneItemByItsLongestSide(
        decimal lengthMeters,
        decimal widthMeters,
        decimal heightMeters,
        string type,
        decimal cost)
    {
        var quote = ShippingCalculator.GetShippingCost(
            [new ShippingItem(lengthMeters, widthMeters, heightMeters)]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(type, line.Type);
        Assert.Equal(cost, line.Cost);
        Assert.Equal(cost, quote.Total);
        Assert.False(string.IsNullOrWhiteSpace(line.Description));
    }

    [Fact]
    public void AddsOneLinePerItemAndSumsTheTotal()
    {
        IReadOnlyList<ShippingItem> items =
        [
            new(0.05m, 0.05m, 0.05m),
            new(0.05m, 0.04m, 0.03m),
            new(0.20m, 0.20m, 0.20m),
            new(0.80m, 0.40m, 0.20m),
            new(1.50m, 0.20m, 0.10m),
        ];

        var quote = ShippingCalculator.GetShippingCost(items);

        Assert.Equal(
            [
                new CostLine("small", 3m, "0.05 m × 0.05 m × 0.05 m"),
                new CostLine("small", 3m, "0.05 m × 0.04 m × 0.03 m"),
                new CostLine("medium", 8m, "0.2 m × 0.2 m × 0.2 m"),
                new CostLine("large", 15m, "0.8 m × 0.4 m × 0.2 m"),
                new CostLine("xl", 25m, "1.5 m × 0.2 m × 0.1 m"),
            ],
            quote.Lines);
        Assert.Equal(54m, quote.Total);
    }

    [Fact]
    public void DescribesEachItemWithItsOwnDimensions()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(1.00m, 0.10m, 0.099m),
        ]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal("1 m × 0.1 m × 0.099 m", line.Description);
    }

    [Fact]
    public void SpeedyShippingAddsALineEqualToTheItemsCostAndDoublesTheTotal()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m),
            new ShippingItem(0.20m, 0.20m, 0.20m),
            new ShippingItem(1.50m, 0.20m, 0.10m),
        ],
        isSpeedyShipping: true);

        Assert.Equal(
            [
                new CostLine("small", 3m, "0.05 m × 0.05 m × 0.05 m"),
                new CostLine("medium", 8m, "0.2 m × 0.2 m × 0.2 m"),
                new CostLine("xl", 25m, "1.5 m × 0.2 m × 0.1 m"),
                new CostLine("speedy", 36m, "Speedy shipping"),
            ],
            quote.Lines);
        Assert.Equal(72m, quote.Total);
    }

    [Fact]
    public void StandardShippingHasNoSpeedyLine()
    {
        IReadOnlyList<ShippingItem> items = [new ShippingItem(0.20m, 0.20m, 0.20m)];

        var standard = ShippingCalculator.GetShippingCost(items, isSpeedyShipping: false);
        var byDefault = ShippingCalculator.GetShippingCost(items);

        Assert.DoesNotContain(standard.Lines, line => line.Type == "speedy");
        Assert.Equal(8m, standard.Total);
        Assert.Equal(standard.Lines, byDefault.Lines);
        Assert.Equal(standard.Total, byDefault.Total);
    }

    [Fact]
    public void SpeedyShippingWithNoItemsAddsAZeroLine()
    {
        var quote = ShippingCalculator.GetShippingCost([], isSpeedyShipping: true);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(new CostLine("speedy", 0m, "Speedy shipping"), line);
        Assert.Equal(0m, quote.Total);
    }

    [Fact]
    public void ReturnsAnEmptyQuoteWhenThereAreNoItems()
    {
        var quote = ShippingCalculator.GetShippingCost([]);

        Assert.Empty(quote.Lines);
        Assert.Equal(0m, quote.Total);
    }

    [Fact]
    public void RejectsNullItems()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ShippingCalculator.GetShippingCost(null!));

        Assert.Equal("items", exception.ParamName);
    }
}
