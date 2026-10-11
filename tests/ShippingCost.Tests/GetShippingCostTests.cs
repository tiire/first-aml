namespace ShippingCost.Tests;

public class GetShippingCostTests
{
    [Theory]
    [InlineData(0.09, 0.09, 0.09, CostLine.Small, 3)]
    [InlineData(0.01, 0.02, 0.08, CostLine.Small, 3)]
    [InlineData(0.10, 0.01, 0.01, CostLine.Medium, 8)]
    [InlineData(0.49, 0.20, 0.10, CostLine.Medium, 8)]
    [InlineData(0.50, 0.10, 0.10, CostLine.Large, 15)]
    [InlineData(0.99, 0.40, 0.20, CostLine.Large, 15)]
    [InlineData(1.00, 0.01, 0.01, CostLine.Xl, 25)]
    [InlineData(2.40, 0.30, 0.05, CostLine.Xl, 25)]
    public void PricesOneItemByItsLongestSide(
        decimal lengthMeters,
        decimal widthMeters,
        decimal heightMeters,
        string type,
        decimal cost)
    {
        var quote = ShippingCalculator.GetShippingCost(
            [new ShippingItem(lengthMeters, widthMeters, heightMeters, weightKilograms: 1m)]);

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
            new(0.05m, 0.05m, 0.05m, 1m),
            new(0.05m, 0.04m, 0.03m, 0.5m),
            new(0.20m, 0.20m, 0.20m, 3m),
            new(0.80m, 0.40m, 0.20m, 6m),
            new(1.50m, 0.20m, 0.10m, 10m),
        ];

        var quote = ShippingCalculator.GetShippingCost(items);

        Assert.Equal(
            [
                new CostLine(CostLine.Small, 3m, "5 cm × 5 cm × 5 cm, 1 kg"),
                new CostLine(CostLine.Small, 3m, "5 cm × 4 cm × 3 cm, 0.5 kg"),
                new CostLine(CostLine.Medium, 8m, "20 cm × 20 cm × 20 cm, 3 kg"),
                new CostLine(CostLine.Large, 15m, "80 cm × 40 cm × 20 cm, 6 kg"),
                new CostLine(CostLine.Xl, 25m, "150 cm × 20 cm × 10 cm, 10 kg"),
            ],
            quote.Lines);
        Assert.Equal(54m, quote.Total);
    }

    [Fact]
    public void DescribesEachItemWithItsOwnDimensions()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(1.00m, 0.10m, 0.099m, 6m),
        ]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal("100 cm × 10 cm × 10 cm, 6 kg", line.Description);
    }

    [Theory]
    [MemberData(nameof(WeightCases))]
    public void AddsTwoDollarsPerKilogramOverTheSizeLimit(
        ShippingItem item,
        string type,
        decimal cost,
        string description)
    {
        var quote = ShippingCalculator.GetShippingCost([item]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(type, line.Type);
        Assert.Equal(cost, line.Cost);
        Assert.Equal(description, line.Description);
        Assert.Equal(cost, quote.Total);
    }

    public static TheoryData<ShippingItem, string, decimal, string> WeightCases() => new()
    {
        { new ShippingItem(0.05m, 0.05m, 0.05m, 0.4m), CostLine.Small, 3m, "5 cm × 5 cm × 5 cm, 0.4 kg" },
        { new ShippingItem(0.05m, 0.05m, 0.05m, 1m), CostLine.Small, 3m, "5 cm × 5 cm × 5 cm, 1 kg" },
        { new ShippingItem(0.05m, 0.05m, 0.05m, 1.5m), CostLine.Small, 5m, "5 cm × 5 cm × 5 cm, 1.5 kg, 1 kg overweight" },
        { new ShippingItem(0.05m, 0.05m, 0.05m, 2m), CostLine.Small, 5m, "5 cm × 5 cm × 5 cm, 2 kg, 1 kg overweight" },
        { new ShippingItem(0.20m, 0.20m, 0.20m, 3m), CostLine.Medium, 8m, "20 cm × 20 cm × 20 cm, 3 kg" },
        { new ShippingItem(0.20m, 0.20m, 0.20m, 4.25m), CostLine.Medium, 12m, "20 cm × 20 cm × 20 cm, 4.25 kg, 2 kg overweight" },
        { new ShippingItem(0.60m, 0.40m, 0.20m, 6m), CostLine.Large, 15m, "60 cm × 40 cm × 20 cm, 6 kg" },
        { new ShippingItem(0.60m, 0.40m, 0.20m, 9m), CostLine.Large, 21m, "60 cm × 40 cm × 20 cm, 9 kg, 3 kg overweight" },
        { new ShippingItem(1.20m, 0.20m, 0.20m, 10m), CostLine.Xl, 25m, "120 cm × 20 cm × 20 cm, 10 kg" },
        { new ShippingItem(1.20m, 0.20m, 0.20m, 12.5m), CostLine.Xl, 31m, "120 cm × 20 cm × 20 cm, 12.5 kg, 3 kg overweight" },
    };

    [Fact]
    public void SpeedyShippingDoublesTheTotalIncludingOverweight()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 2.5m),
            new ShippingItem(1.20m, 0.20m, 0.20m, 11m),
        ],
        isSpeedyShipping: true);

        Assert.Equal(
            [
                new CostLine(CostLine.Small, 7m, "5 cm × 5 cm × 5 cm, 2.5 kg, 2 kg overweight"),
                new CostLine(CostLine.Xl, 27m, "120 cm × 20 cm × 20 cm, 11 kg, 1 kg overweight"),
                new CostLine(CostLine.Speedy, 34m, "Speedy shipping"),
            ],
            quote.Lines);
        Assert.Equal(68m, quote.Total);
    }

    [Fact]
    public void SpeedyShippingAddsALineEqualToTheItemsCostAndDoublesTheTotal()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.20m, 0.20m, 0.20m, 3m),
            new ShippingItem(1.50m, 0.20m, 0.10m, 10m),
        ],
        isSpeedyShipping: true);

        Assert.Equal(
            [
                new CostLine(CostLine.Small, 3m, "5 cm × 5 cm × 5 cm, 1 kg"),
                new CostLine(CostLine.Medium, 8m, "20 cm × 20 cm × 20 cm, 3 kg"),
                new CostLine(CostLine.Xl, 25m, "150 cm × 20 cm × 10 cm, 10 kg"),
                new CostLine(CostLine.Speedy, 36m, "Speedy shipping"),
            ],
            quote.Lines);
        Assert.Equal(72m, quote.Total);
    }

    [Fact]
    public void StandardShippingHasNoSpeedyLine()
    {
        IReadOnlyList<ShippingItem> items = [new ShippingItem(0.20m, 0.20m, 0.20m, 3m)];

        var standard = ShippingCalculator.GetShippingCost(items, isSpeedyShipping: false);
        var byDefault = ShippingCalculator.GetShippingCost(items);

        Assert.DoesNotContain(standard.Lines, line => line.Type == CostLine.Speedy);
        Assert.Equal(8m, standard.Total);
        Assert.Equal(standard.Lines, byDefault.Lines);
        Assert.Equal(standard.Total, byDefault.Total);
    }

    [Fact]
    public void SpeedyShippingWithNoItemsAddsAZeroLine()
    {
        var quote = ShippingCalculator.GetShippingCost([], isSpeedyShipping: true);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(new CostLine(CostLine.Speedy, 0m, "Speedy shipping"), line);
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
    public void RoundsDimensionsToCentimetersAndWeightToGramsBeforePricing()
    {
        var roundsUpToMedium = ShippingCalculator.GetShippingCost(
            [new ShippingItem(0.095m, 0.01m, 0.01m, 1m)]);
        var staysSmall = ShippingCalculator.GetShippingCost(
            [new ShippingItem(0.094m, 0.01m, 0.01m, 1.0004m)]);
        var roundsWeightUp = ShippingCalculator.GetShippingCost(
            [new ShippingItem(0.05m, 0.05m, 0.05m, 1.0005m)]);

        var medium = Assert.Single(roundsUpToMedium.Lines);
        Assert.Equal(CostLine.Medium, medium.Type);
        Assert.Equal(8m, medium.Cost);
        Assert.Equal("10 cm × 1 cm × 1 cm, 1 kg", medium.Description);

        var small = Assert.Single(staysSmall.Lines);
        Assert.Equal(CostLine.Small, small.Type);
        Assert.Equal(3m, small.Cost);
        Assert.Equal("9 cm × 1 cm × 1 cm, 1 kg", small.Description);

        var overweight = Assert.Single(roundsWeightUp.Lines);
        Assert.Equal(CostLine.Small, overweight.Type);
        Assert.Equal(5m, overweight.Cost);
        Assert.Equal("5 cm × 5 cm × 5 cm, 1.001 kg, 1 kg overweight", overweight.Description);
    }

    [Theory]
    [InlineData(0.495, CostLine.Large, 15, "50 cm × 10 cm × 10 cm, 1 kg")]
    [InlineData(0.995, CostLine.Xl, 25, "100 cm × 10 cm × 10 cm, 1 kg")]
    public void RoundsUpOntoTheNextSizeBoundary(
        decimal lengthMeters,
        string type,
        decimal cost,
        string description)
    {
        var quote = ShippingCalculator.GetShippingCost(
            [new ShippingItem(lengthMeters, 0.10m, 0.10m, 1m)]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(type, line.Type);
        Assert.Equal(cost, line.Cost);
        Assert.Equal(description, line.Description);
        Assert.Equal(cost, quote.Total);
    }

    [Fact]
    public void RoundsAHalfCentimeterUpAndPricesIt()
    {
        var quote = ShippingCalculator.GetShippingCost(
            [new ShippingItem(0.005m, 0.01m, 0.01m, 1m)]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(CostLine.Small, line.Type);
        Assert.Equal(3m, line.Cost);
        Assert.Equal("1 cm × 1 cm × 1 cm, 1 kg", line.Description);
    }

    [Fact]
    public void RoundsAHalfGramUpAndPricesIt()
    {
        var quote = ShippingCalculator.GetShippingCost(
            [new ShippingItem(0.05m, 0.05m, 0.05m, 0.0005m)]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(CostLine.Small, line.Type);
        Assert.Equal(3m, line.Cost);
        Assert.Equal("5 cm × 5 cm × 5 cm, 0.001 kg", line.Description);
    }

    [Fact]
    public void ReturnsLinesThatCannotBeChanged()
    {
        var quote = ShippingCalculator.GetShippingCost(
            [new ShippingItem(0.05m, 0.05m, 0.05m, 1m)]);

        var lines = Assert.IsAssignableFrom<IList<CostLine>>(quote.Lines);
        Assert.True(lines.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => lines[0] = new CostLine(CostLine.Small, 0m, "changed"));
        Assert.Equal(3m, quote.Lines[0].Cost);
        Assert.Equal(3m, quote.Total);
    }

    [Fact]
    public void RejectsNullItems()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ShippingCalculator.GetShippingCost(null!));

        Assert.Equal("items", exception.ParamName);
    }

    [Fact]
    public void RejectsANullItemInTheList()
    {
        ShippingItem?[] items =
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            null,
        ];

        var exception = Assert.Throws<ArgumentException>(
            () => ShippingCalculator.GetShippingCost(items!));

        Assert.Equal("items", exception.ParamName);
        Assert.Contains("index 1", exception.Message);
    }
}
