namespace ShippingCost.Tests;

public class ParcelDiscountTests
{
    [Fact]
    public void MakesTheCheapestSmallParcelInEachGroupOfFourFree()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1.5m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 2m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 2.5m),
        ]);

        Assert.Equal(
            [
                new CostLine(CostLine.Small, 3m, "5 cm × 5 cm × 5 cm, 1 kg"),
                new CostLine(CostLine.Small, 5m, "5 cm × 5 cm × 5 cm, 1.5 kg, 1 kg overweight"),
                new CostLine(CostLine.Small, 5m, "5 cm × 5 cm × 5 cm, 2 kg, 1 kg overweight"),
                new CostLine(CostLine.Small, 7m, "5 cm × 5 cm × 5 cm, 2.5 kg, 2 kg overweight"),
                new CostLine(CostLine.SmallMania, -3m, "Small parcel mania: every 4th small parcel is free"),
            ],
            quote.Lines);
        Assert.Equal(17m, quote.Total);
    }

    [Fact]
    public void MakesTheCheapestMediumParcelInEachGroupOfThreeFree()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.20m, 0.20m, 0.20m, 3m),
            new ShippingItem(0.20m, 0.20m, 0.20m, 4.25m),
            new ShippingItem(0.20m, 0.20m, 0.20m, 3m),
        ]);

        Assert.Equal(
            [
                new CostLine(CostLine.Medium, 8m, "20 cm × 20 cm × 20 cm, 3 kg"),
                new CostLine(CostLine.Medium, 12m, "20 cm × 20 cm × 20 cm, 4.25 kg, 2 kg overweight"),
                new CostLine(CostLine.Medium, 8m, "20 cm × 20 cm × 20 cm, 3 kg"),
                new CostLine(CostLine.MediumMania, -8m, "Medium parcel mania: every 3rd medium parcel is free"),
            ],
            quote.Lines);
        Assert.Equal(20m, quote.Total);
    }

    [Fact]
    public void AppliesSmallAndMediumManiaTogether()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.20m, 0.20m, 0.20m, 3m),
            new ShippingItem(0.20m, 0.20m, 0.20m, 3m),
            new ShippingItem(0.20m, 0.20m, 0.20m, 3m),
        ]);

        Assert.Equal(4, quote.Lines.Count(line => line.Type == CostLine.Small && line.Cost == 3m));
        Assert.Equal(3, quote.Lines.Count(line => line.Type == CostLine.Medium && line.Cost == 8m));
        Assert.Equal(
            [
                new CostLine(CostLine.SmallMania, -3m, "Small parcel mania: every 4th small parcel is free"),
                new CostLine(CostLine.MediumMania, -8m, "Medium parcel mania: every 3rd medium parcel is free"),
            ],
            quote.Lines.TakeLast(2));
        Assert.Equal(25m, quote.Total);
    }

    [Fact]
    public void UsesMixedManiaOnExpensiveParcelsAndSmallManiaOnTheRest()
    {
        ShippingItem[] items =
        [
            ..Enumerable.Repeat(new ShippingItem(0.05m, 0.05m, 0.05m, 15m), 4),
            ..Enumerable.Repeat(new ShippingItem(1.20m, 0.20m, 0.20m, 20m), 5),
        ];

        var quote = ShippingCalculator.GetShippingCost(items);

        Assert.Equal(4, quote.Lines.Count(line => line.Type == CostLine.Small && line.Cost == 31m));
        Assert.Equal(5, quote.Lines.Count(line => line.Type == CostLine.Xl && line.Cost == 45m));
        Assert.Equal(
            [
                new CostLine(CostLine.SmallMania, -31m, "Small parcel mania: every 4th small parcel is free"),
                new CostLine(CostLine.MixedMania, -45m, "Mixed parcel mania: every 5th parcel is free"),
            ],
            quote.Lines.TakeLast(2));
        Assert.Equal(273m, quote.Total);
    }

    [Fact]
    public void FreesTheCheapestParcelInTheExpensiveGroupRatherThanTheCheapestOverall()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 7m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 6m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 5m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 4m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 3m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 2m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
        ]);

        Assert.Equal(
            [15m, 13m, 11m, 9m, 7m, 5m, 3m, -9m],
            quote.Lines.Select(line => line.Cost));
        Assert.Equal(CostLine.SmallMania, quote.Lines[^1].Type);
        Assert.Equal("Small parcel mania: every 4th small parcel is free", quote.Lines[^1].Description);
        Assert.Equal(54m, quote.Total);
    }

    [Fact]
    public void SpeedyShippingDoublesTheDiscountedItemTotal()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
        ],
        isSpeedyShipping: true);

        Assert.Equal([3m, 3m, 3m, 3m, -3m, 9m], quote.Lines.Select(line => line.Cost));
        Assert.Equal(CostLine.SmallMania, quote.Lines[^2].Type);
        Assert.Equal(CostLine.Speedy, quote.Lines[^1].Type);
        Assert.Equal(18m, quote.Total);
    }

    [Fact]
    public void DoesNotDiscountFewerThanAFullGroup()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
            new ShippingItem(0.05m, 0.05m, 0.05m, 1m),
        ]);

        Assert.Equal(3, quote.Lines.Count);
        Assert.Equal(9m, quote.Total);
        Assert.All(quote.Lines, line => Assert.Equal(3m, line.Cost));
    }

    [Fact]
    public void KeepsTheStandardPriceWhenMediumManiaMakesItCheaperThanHeavy()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
        ]);

        Assert.Equal(
            [
                new CostLine(CostLine.Medium, 52m, "30 cm × 30 cm × 30 cm, 25 kg, 22 kg overweight"),
                new CostLine(CostLine.Medium, 52m, "30 cm × 30 cm × 30 cm, 25 kg, 22 kg overweight"),
                new CostLine(CostLine.Medium, 52m, "30 cm × 30 cm × 30 cm, 25 kg, 22 kg overweight"),
                new CostLine(CostLine.MediumMania, -52m, "Medium parcel mania: every 3rd medium parcel is free"),
            ],
            quote.Lines);
        Assert.Equal(104m, quote.Total);
    }

    [Fact]
    public void KeepsTheHeavyPriceWhenNoDiscountMakesStandardCheaper()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
        ]);

        Assert.All(quote.Lines, line => Assert.Equal(CostLine.Heavy, line.Type));
        Assert.Equal(100m, quote.Total);
    }

    [Fact]
    public void MixesHeavyAndStandardPricesInTheSameOrder()
    {
        var quote = ShippingCalculator.GetShippingCost(
        [
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
            new ShippingItem(0.3m, 0.3m, 0.3m, 25m),
            new ShippingItem(0.3m, 0.3m, 0.3m, 40m),
        ]);

        Assert.Equal(CostLine.Medium, quote.Lines[0].Type);
        Assert.Equal(CostLine.Medium, quote.Lines[1].Type);
        Assert.Equal(CostLine.Medium, quote.Lines[2].Type);
        Assert.Equal(new CostLine(CostLine.Heavy, 50m, "30 cm × 30 cm × 30 cm, 40 kg"), quote.Lines[3]);
        Assert.Equal(CostLine.MediumMania, quote.Lines[^1].Type);
        Assert.Equal(154m, quote.Total);
    }
}
