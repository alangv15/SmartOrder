using System.Globalization;
using System.Text;

namespace SmartOrder.Shared.Printing;

public sealed class EscPosTicketFormatter
{
    private const int TicketColumns = 48;
    private const int NoDiscountProductColumnWidth = 24;
    private const int NoDiscountQuantityColumnWidth = 4;
    private const int NoDiscountUnitPriceColumnWidth = 10;
    private const int NoDiscountTotalColumnWidth = 10;
    private const int DiscountProductColumnWidth = 21;
    private const int QuantityColumnWidth = 4;
    private const int DiscountUnitPriceColumnWidth = 7;
    private const int DiscountColumnWidth = 8;
    private const int DiscountTotalColumnWidth = 8;

    public byte[] Format(TicketDocument document, byte[]? logoBytes = null)
    {
        var bytes = new List<byte>();

        Add(bytes, 0x1B, 0x40); // Initialize printer.
        Add(bytes, 0x1B, 0x61, 0x01); // Center.

        if (logoBytes?.Length > 0)
        {
            bytes.AddRange(logoBytes);
            AddLine(bytes, string.Empty);
        }

        Add(bytes, 0x1B, 0x21, 0x08); // Emphasized.
        AddLine(bytes, document.Title);
        Add(bytes, 0x1B, 0x21, 0x00);

        foreach (var line in document.HeaderLines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            AddLine(bytes, line);
        }

        AddLine(bytes, string.Empty);
        Add(bytes, 0x1B, 0x61, 0x00); // Left.

        foreach (var line in document.InfoLines)
        {
            AddWrappedLine(bytes, $"{line.Label}: {line.Value}", TicketColumns);
        }

        AddSeparator(bytes);
        AddLine(bytes, document.IncludeDiscounts
            ? FormatDiscountItemHeaderColumns("Producto", "Cant", "P.Unit", "Desc", "Total")
            : FormatNoDiscountItemHeaderColumns("Producto", "Cant", "P.Unit", "Total"));
        AddSeparator(bytes);

        var productColumnWidth = document.IncludeDiscounts
            ? DiscountProductColumnWidth
            : NoDiscountProductColumnWidth;

        foreach (var group in document.Items
            .OrderBy(item => Normalize(item.Category))
            .ThenBy(item => Normalize(item.Description))
            .GroupBy(item => Normalize(item.Category)))
        {
            var category = string.IsNullOrWhiteSpace(group.Key) ? "Sin categoria" : group.Key;
            AddLine(bytes, Truncate(category, productColumnWidth));

            foreach (var item in group)
            {
                AddLine(bytes, document.IncludeDiscounts
                    ? FormatDiscountItemColumns(
                        item.Description,
                        item.Quantity.ToString(CultureInfo.InvariantCulture),
                        Money(item.UnitPrice),
                        Money(item.Discount),
                        Money(item.Total))
                    : FormatNoDiscountItemColumns(
                        item.Description,
                        item.Quantity.ToString(CultureInfo.InvariantCulture),
                        Money(item.UnitPrice),
                        Money(item.Total)));
            }
        }

        AddSeparator(bytes);
        foreach (var total in document.Totals)
        {
            if (total.IsGrandTotal)
            {
                Add(bytes, 0x1B, 0x21, 0x08);
            }

            AddLine(bytes, FitColumns(total.Label, Money(total.Amount)));

            if (total.IsGrandTotal)
            {
                Add(bytes, 0x1B, 0x21, 0x00);
            }
        }

        AddLine(bytes, string.Empty);
        Add(bytes, 0x1B, 0x61, 0x01); // Center.
        foreach (var line in document.FooterLines.Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            AddWrappedLine(bytes, line, TicketColumns);
        }

        AddLine(bytes, string.Empty);
        AddLine(bytes, string.Empty);
        Add(bytes, 0x1D, 0x56, 0x42, 0x00); // Partial cut.

        return bytes.ToArray();
    }

    private static void AddSeparator(ICollection<byte> bytes)
    {
        AddLine(bytes, new string('-', TicketColumns));
    }

    private static void AddWrappedLine(ICollection<byte> bytes, string value, int maxLength)
    {
        foreach (var line in Wrap(value, maxLength))
        {
            AddLine(bytes, line);
        }
    }

    private static void AddLine(ICollection<byte> bytes, string value)
    {
        foreach (var currentByte in Encoding.ASCII.GetBytes(NormalizeForPrinter(value)))
        {
            bytes.Add(currentByte);
        }

        bytes.Add(0x0A);
    }

    private static void Add(ICollection<byte> bytes, params byte[] values)
    {
        foreach (var value in values)
        {
            bytes.Add(value);
        }
    }

    private static string FitColumns(string left, string right)
    {
        var normalizedLeft = Normalize(left);
        var normalizedRight = Normalize(right);
        var space = Math.Max(1, TicketColumns - normalizedLeft.Length - normalizedRight.Length);
        var line = $"{normalizedLeft}{new string(' ', space)}{normalizedRight}";
        return line.Length <= TicketColumns ? line : line[..TicketColumns];
    }

    private static string FormatNoDiscountItemColumns(string product, string quantity, string unitPrice, string total)
    {
        return
            PadRight(product, NoDiscountProductColumnWidth) +
            PadLeft(quantity, NoDiscountQuantityColumnWidth) +
            PadLeft(unitPrice, NoDiscountUnitPriceColumnWidth) +
            PadLeft(total, NoDiscountTotalColumnWidth);
    }

    private static string FormatDiscountItemColumns(string product, string quantity, string unitPrice, string discount, string total)
    {
        return
            PadRight(product, DiscountProductColumnWidth) +
            PadLeft(quantity, QuantityColumnWidth) +
            PadLeft(unitPrice, DiscountUnitPriceColumnWidth) +
            PadLeft(discount, DiscountColumnWidth) +
            PadLeft(total, DiscountTotalColumnWidth);
    }

    private static string FormatNoDiscountItemHeaderColumns(string product, string quantity, string unitPrice, string total)
    {
        return
            PadRight(product, NoDiscountProductColumnWidth) +
            PadRight(quantity, NoDiscountQuantityColumnWidth) +
            PadCenter(unitPrice, NoDiscountUnitPriceColumnWidth) +
            PadLeft(total, NoDiscountTotalColumnWidth);
    }

    private static string FormatDiscountItemHeaderColumns(string product, string quantity, string unitPrice, string discount, string total)
    {
        return
            PadRight(product, DiscountProductColumnWidth) +
            PadRight(quantity, QuantityColumnWidth) +
            PadLeft(unitPrice, DiscountUnitPriceColumnWidth) +
            PadCenter(discount, DiscountColumnWidth) +
            PadLeft(total, DiscountTotalColumnWidth);
    }

    private static string PadRight(string value, int width)
    {
        return Truncate(value, width).PadRight(width);
    }

    private static string PadLeft(string value, int width)
    {
        return Truncate(value, width).PadLeft(width);
    }

    private static string PadCenter(string value, int width)
    {
        var truncated = Truncate(value, width);
        var leftPadding = Math.Max(0, (width - truncated.Length) / 2);
        var rightPadding = Math.Max(0, width - truncated.Length - leftPadding);
        return $"{new string(' ', leftPadding)}{truncated}{new string(' ', rightPadding)}";
    }

    private static string Truncate(string value, int maxLength)
    {
        var normalized = Normalize(value);
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static IEnumerable<string> Wrap(string value, int maxLength)
    {
        var words = Normalize(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();

        foreach (var word in words)
        {
            if (line.Length > 0 && line.Length + word.Length + 1 > maxLength)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }

    private static string Money(decimal value)
    {
        return value.ToString("$#,##0.00", CultureInfo.InvariantCulture);
    }

    private static string Normalize(string value)
    {
        return RemoveUnsupportedCharacters(string.Join(' ', (value ?? string.Empty)
                .Trim()
                .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)));
    }

    private static string NormalizeForPrinter(string value)
    {
        return RemoveUnsupportedCharacters(value ?? string.Empty);
    }

    private static string RemoveUnsupportedCharacters(string value)
    {
        return value
            .Replace("á", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("é", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("í", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ó", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ú", "u", StringComparison.OrdinalIgnoreCase)
            .Replace("ñ", "n", StringComparison.OrdinalIgnoreCase);
    }
}
