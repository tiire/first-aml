namespace ShippingCost.Tests;

public class ShippingItemTests
{
    [Theory]
    [InlineData(0, 0.1, 0.1, 1, "LengthMeters")]
    [InlineData(-0.2, 0.1, 0.1, 1, "LengthMeters")]
    [InlineData(0.1, 0, 0.1, 1, "WidthMeters")]
    [InlineData(0.1, -0.2, 0.1, 1, "WidthMeters")]
    [InlineData(0.1, 0.1, 0, 1, "HeightMeters")]
    [InlineData(0.1, 0.1, -0.2, 1, "HeightMeters")]
    public void RejectsNonPositiveDimensions(
        decimal lengthMeters,
        decimal widthMeters,
        decimal heightMeters,
        decimal weightKilograms,
        string paramName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShippingItem(lengthMeters, widthMeters, heightMeters, weightKilograms));

        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public void RejectsNegativeWeight()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShippingItem(0.1m, 0.2m, 0.3m, -0.01m));

        Assert.Equal("WeightKilograms", exception.ParamName);
    }

    [Fact]
    public void RejectsZeroWeight()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShippingItem(0.1m, 0.2m, 0.3m, 0m));

        Assert.Equal("WeightKilograms", exception.ParamName);
    }

    [Fact]
    public void RejectsInvalidValuesSetAfterConstruction()
    {
        var item = new ShippingItem(0.1m, 0.2m, 0.3m, 1m);

        var length = Assert.Throws<ArgumentOutOfRangeException>(() => item with { LengthMeters = 0m });
        var zeroWeight = Assert.Throws<ArgumentOutOfRangeException>(() => item with { WeightKilograms = 0m });
        var negativeWeight = Assert.Throws<ArgumentOutOfRangeException>(() => item with { WeightKilograms = -1m });

        Assert.Equal("LengthMeters", length.ParamName);
        Assert.Equal("WeightKilograms", zeroWeight.ParamName);
        Assert.Equal("WeightKilograms", negativeWeight.ParamName);
    }

    [Theory]
    [InlineData(4.1, 1, 1, 1, "LengthMeters")]
    [InlineData(1, 5, 1, 1, "WidthMeters")]
    [InlineData(1, 1, 4.5, 1, "HeightMeters")]
    public void RejectsDimensionsAboveTheCap(
        decimal lengthMeters,
        decimal widthMeters,
        decimal heightMeters,
        decimal weightKilograms,
        string paramName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShippingItem(lengthMeters, widthMeters, heightMeters, weightKilograms));

        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public void RejectsWeightAboveTheCap()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShippingItem(1m, 1m, 1m, 100.01m));

        Assert.Equal("WeightKilograms", exception.ParamName);
    }

    [Fact]
    public void AllowsTheMaximumDimensionAndWeight()
    {
        var item = new ShippingItem(4m, 1m, 1m, 100m);

        var quote = ShippingCalculator.GetShippingCost([item]);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(CostLine.Xl, line.Type);
        Assert.Equal(205m, line.Cost);
        Assert.Equal("400 cm × 100 cm × 100 cm, 100 kg, 90 kg overweight", line.Description);
    }

    [Fact]
    public void RejectsValuesAboveTheCapSetAfterConstruction()
    {
        var item = new ShippingItem(1m, 1m, 1m, 1m);

        var length = Assert.Throws<ArgumentOutOfRangeException>(() => item with { LengthMeters = 4.01m });
        var weight = Assert.Throws<ArgumentOutOfRangeException>(() => item with { WeightKilograms = 101m });

        Assert.Equal("LengthMeters", length.ParamName);
        Assert.Equal("WeightKilograms", weight.ParamName);
    }

    [Fact]
    public void StoresMeasurementsRoundedToCentimetersAndGrams()
    {
        var item = new ShippingItem(0.095m, 0.0944m, 0.005m, 1.0005m);
        var equivalent = new ShippingItem(0.10m, 0.09m, 0.01m, 1.001m);

        Assert.Equal(0.10m, item.LengthMeters);
        Assert.Equal(0.09m, item.WidthMeters);
        Assert.Equal(0.01m, item.HeightMeters);
        Assert.Equal(1.001m, item.WeightKilograms);
        Assert.Equal(equivalent, item);
    }

    [Theory]
    [InlineData(0.004, 0.1, 0.1, 1, "LengthMeters")]
    [InlineData(0.1, 0.004, 0.1, 1, "WidthMeters")]
    [InlineData(0.1, 0.1, 0.004, 1, "HeightMeters")]
    public void RejectsADimensionThatRoundsToZeroCentimeters(
        decimal lengthMeters,
        decimal widthMeters,
        decimal heightMeters,
        decimal weightKilograms,
        string paramName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShippingItem(lengthMeters, widthMeters, heightMeters, weightKilograms));

        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public void RejectsAWeightThatRoundsToZeroGrams()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShippingItem(0.1m, 0.1m, 0.1m, 0.0004m));

        Assert.Equal("WeightKilograms", exception.ParamName);
    }

    [Fact]
    public void RoundsValuesSetAfterConstruction()
    {
        var item = new ShippingItem(0.1m, 0.2m, 0.3m, 1m);

        var rounded = item with { LengthMeters = 0.095m, WeightKilograms = 1.0005m };
        var rejected = Assert.Throws<ArgumentOutOfRangeException>(() => item with { WidthMeters = 0.004m });

        Assert.Equal(0.10m, rounded.LengthMeters);
        Assert.Equal(1.001m, rounded.WeightKilograms);
        Assert.Equal("WidthMeters", rejected.ParamName);
    }
}
