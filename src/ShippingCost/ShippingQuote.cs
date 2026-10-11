namespace ShippingCost;

public sealed record CostLine(string Type, decimal Cost, string Description);

public sealed record ShippingQuote(IReadOnlyList<CostLine> Lines, decimal Total);
