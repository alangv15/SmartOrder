using SmartOrder.Shared.Models;

namespace SmartOrder.Shared.Catalogs
{
    public static class OrderCatalog
    {
        public const string CompletedOrderStatusCode = "Completed";
        public const string CancelledOrderStatusCode = "Cancelled";
        public const string PaidPaymentStatusCode = "Paid";
        public const string InStoreSalesChannelCode = "In-Store";
        public const string CustomOrderSalesChannelCode = "Order";
        public const string CashPaymentMethodCode = "Cash";
        public const string CardPaymentMethodCode = "Card";
        public const string InternalPaymentMethodCode = "Internal";
        public const string NotApplicablePaymentStatusCode = "NotApplicable";
        public const string MaleCustomerGenderCode = "M";
        public const string FemaleCustomerGenderCode = "F";
        public const string NewCustomerTypeCode = "New";
        public const string ReturningCustomerTypeCode = "Returning";
        public const string WalkInAcquisitionChannelCode = "Walk-in";
        public const string RecommendationAcquisitionChannelCode = "Recommendation";
        public const string FacebookAcquisitionChannelCode = "Facebook";
        public const string InstagramAcquisitionChannelCode = "Instagram";
        public const string TiktokAcquisitionChannelCode = "Tiktok";

        public static IReadOnlyList<LookupOption> CustomerGenderOptions { get; } = new List<LookupOption>
        {
            new("Hombre", MaleCustomerGenderCode),
            new("Mujer", FemaleCustomerGenderCode)
        };

        public static IReadOnlyList<LookupOption> CustomerTypeOptions { get; } = new List<LookupOption>
        {
            new("Nuevo", NewCustomerTypeCode),
            new("Habitual", ReturningCustomerTypeCode)
        };

        public static IReadOnlyList<LookupOption> AcquisitionChannelOptions { get; } = new List<LookupOption>
        {
            new("Transeúnte", WalkInAcquisitionChannelCode),
            new("Recomendacion", RecommendationAcquisitionChannelCode),
            new("Facebook", FacebookAcquisitionChannelCode),
            new("Instagram", InstagramAcquisitionChannelCode),
            new("Tiktok", TiktokAcquisitionChannelCode)
        };

        public static IReadOnlyList<LookupOption> PaymentMethodOptions { get; } = new List<LookupOption>
        {
            new("Efectivo", CashPaymentMethodCode),
            new("Tarjeta", CardPaymentMethodCode)
        };

        public static IReadOnlyList<LookupOption> CustomerAgeRangeOptions { get; } = BuildAgeRangeOptions();

        public static string GetPaymentMethodLabel(string? code)
        {
            return PaymentMethodOptions.FirstOrDefault(option => option.Code == code)?.Label ?? code ?? string.Empty;
        }

        public static string GetPaymentMethodColorHex(string? code)
        {
            return code switch
            {
                CashPaymentMethodCode => "#0B6E4F",
                CardPaymentMethodCode => "#1B587C",
                _ => "#486581"
            };
        }

        private static IReadOnlyList<LookupOption> BuildAgeRangeOptions()
        {
            var options = new List<LookupOption>();
            for (var age = 10; age <= 80; age += 10)
            {
                var range = $"{age}-{age + 9}";
                options.Add(new LookupOption(range, range));
            }

            return options;
        }
    }
}
