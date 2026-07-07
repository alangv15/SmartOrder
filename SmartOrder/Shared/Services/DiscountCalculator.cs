using SmartOrder.Entities.Orders.Models;

namespace SmartOrder.Shared.Services;

public sealed record DiscountLineInput(
    int ProductId,
    int CategoryId,
    int Quantity,
    decimal UnitPrice);

public static class DiscountCalculator
{
    private const string FixedAmountTypeCode = "FixedAmount";
    private const string PercentageTypeCode = "Percentage";
    private const string ProductTargetType = "Product";
    private const string CategoryTargetType = "Category";
    private const string AllProductsTargetCode = "AllProducts";
    private const string CapAmountAdjustmentTypeCode = "CapAmount";
    private const string PercentageAdjustmentTypeCode = "Percentage";

    public static decimal CalculateDiscountPerUnit(
        DiscountLineInput line,
        IEnumerable<DiscountRuleDto> selectedDiscounts,
        IEnumerable<DiscountLimitRuleDto> discountLimits,
        int totalQuantity,
        decimal subtotal,
        DateOnly businessDate)
    {
        var applicableRules = selectedDiscounts
            .Where(rule => IsRuleEligible(rule, totalQuantity, subtotal, businessDate))
            .Where(rule => AppliesToLine(rule, line))
            .ToList();

        if (applicableRules.Count == 0)
        {
            return 0;
        }

        var rawDiscount = applicableRules.Sum(rule => CalculateRuleDiscountPerUnit(rule, line.UnitPrice));
        var adjustedDiscount = ApplyDiscountAdjustments(rawDiscount, discountLimits, line, businessDate);

        return Math.Round(Math.Min(adjustedDiscount, line.UnitPrice), 2, MidpointRounding.AwayFromZero);
    }

    private static bool IsRuleEligible(DiscountRuleDto rule, int totalQuantity, decimal subtotal, DateOnly businessDate)
    {
        if (!rule.IsActive || rule.DiscountRuleId <= 0)
        {
            return false;
        }

        if (rule.MinQuantity.HasValue && totalQuantity < rule.MinQuantity.Value)
        {
            return false;
        }

        if (rule.MinTotalAmount.HasValue && subtotal < rule.MinTotalAmount.Value)
        {
            return false;
        }

        if (rule.StartDate.HasValue && businessDate < rule.StartDate.Value)
        {
            return false;
        }

        if (rule.EndDate.HasValue && businessDate > rule.EndDate.Value)
        {
            return false;
        }

        return true;
    }

    private static bool AppliesToLine(DiscountRuleDto rule, DiscountLineInput line)
    {
        var targetCode = rule.DiscountTargetCode?.Trim() ?? string.Empty;
        if (targetCode.Equals(AllProductsTargetCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (targetCode.Equals(ProductTargetType, StringComparison.OrdinalIgnoreCase))
        {
            return rule.DiscountRuleTargets.Any(target =>
                IsTargetType(target.TargetType, ProductTargetType) &&
                target.TargetId == line.ProductId);
        }

        if (targetCode.Equals(CategoryTargetType, StringComparison.OrdinalIgnoreCase))
        {
            return rule.DiscountRuleTargets.Any(target =>
                IsTargetType(target.TargetType, CategoryTargetType) &&
                target.TargetId == line.CategoryId);
        }

        return true;
    }

    private static decimal CalculateRuleDiscountPerUnit(DiscountRuleDto rule, decimal unitPrice)
    {
        if (rule.DiscountTypeCode.Equals(PercentageTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            return unitPrice * (rule.DiscountValue / 100m);
        }

        if (rule.DiscountTypeCode.Equals(FixedAmountTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            return rule.DiscountValue;
        }

        return rule.DiscountValue;
    }

    private static decimal ApplyDiscountAdjustments(
        decimal rawDiscount,
        IEnumerable<DiscountLimitRuleDto> limits,
        DiscountLineInput line,
        DateOnly businessDate)
    {
        var matchingLimits = limits
            .Where(limit => limit.IsActive)
            .Where(limit => IsLimitInDateRange(limit, businessDate))
            .Where(limit => limit.DiscountLimitTargets.Any(target =>
                (IsTargetType(target.TargetType, ProductTargetType) && target.TargetId == line.ProductId) ||
                (IsTargetType(target.TargetType, CategoryTargetType) && target.TargetId == line.CategoryId)))
            .ToList();

        var adjustedDiscount = rawDiscount;
        foreach (var limit in matchingLimits)
        {
            var adjustmentType = string.IsNullOrWhiteSpace(limit.AdjustmentTypeCode)
                ? CapAmountAdjustmentTypeCode
                : limit.AdjustmentTypeCode.Trim();
            var adjustmentValue = limit.AdjustmentValue > 0 ? limit.AdjustmentValue : limit.MaxDiscountPerUnit;

            if (adjustmentType.Equals(PercentageAdjustmentTypeCode, StringComparison.OrdinalIgnoreCase))
            {
                adjustedDiscount = Math.Min(adjustedDiscount, rawDiscount * (adjustmentValue / 100m));
                continue;
            }

            if (adjustmentType.Equals(CapAmountAdjustmentTypeCode, StringComparison.OrdinalIgnoreCase))
            {
                adjustedDiscount = Math.Min(adjustedDiscount, adjustmentValue);
            }
        }

        return adjustedDiscount;
    }

    private static bool IsLimitInDateRange(DiscountLimitRuleDto limit, DateOnly businessDate)
    {
        if (limit.StartDate.HasValue && businessDate < limit.StartDate.Value)
        {
            return false;
        }

        if (limit.EndDate.HasValue && businessDate > limit.EndDate.Value)
        {
            return false;
        }

        return true;
    }

    private static bool IsTargetType(string? value, string expected)
    {
        return string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
