namespace SmartOrder.Shared.Printing;

public sealed record TicketDocument(
    string Title,
    IReadOnlyList<string> HeaderLines,
    IReadOnlyList<TicketInfoLine> InfoLines,
    IReadOnlyList<TicketItemLine> Items,
    bool IncludeDiscounts,
    IReadOnlyList<TicketTotalLine> Totals,
    IReadOnlyList<string> FooterLines);

public sealed record TicketInfoLine(string Label, string Value);

public sealed record TicketItemLine(
    string Category,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total);

public sealed record TicketTotalLine(string Label, decimal Amount, bool IsGrandTotal = false);
