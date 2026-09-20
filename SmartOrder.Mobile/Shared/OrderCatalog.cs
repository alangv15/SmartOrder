namespace SmartOrder.Mobile.Shared;

public static class OrderCatalog
{
    public const string CustomOrderSalesChannelCode = "Order";
    public const string CashPaymentMethodCode = "Cash";
    public const string CardPaymentMethodCode = "Card";
    public const string InternalPaymentMethodCode = "Internal";
    public const string NotApplicablePaymentStatusCode = "NotApplicable";

    public static IReadOnlyList<LookupOption> PaymentMethodOptions { get; } = new List<LookupOption>
    {
        new("Efectivo", CashPaymentMethodCode),
        new("Tarjeta", CardPaymentMethodCode)
    };
}
