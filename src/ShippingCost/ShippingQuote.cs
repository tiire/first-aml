namespace ShippingCost;

public sealed record CostLine(string Type, decimal Cost);

public sealed record ShippingQuote(IReadOnlyList<CostLine> Lines, decimal Total);
