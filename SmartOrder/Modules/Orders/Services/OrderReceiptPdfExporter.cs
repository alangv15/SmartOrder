using SmartOrder.Modules.Orders.ViewModels;
using System.Globalization;
using System.Text;

namespace SmartOrder.Modules.Orders.Services;

public sealed record OrderReceiptPdfRequest(
    int? Folio,
    string BranchName,
    string? BranchAddress,
    string? BranchPhone,
    string CustomerName,
    bool IsInternalProduction,
    DateTime DeliveryDate,
    TimeSpan DeliveryTime,
    string? Comments,
    IReadOnlyList<OrderItemViewModel> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total);

public static class OrderReceiptPdfExporter
{
    private const int PageWidth = 612;
    private const int PageHeight = 792;
    private const int Margin = 46;
    private const int LineHeight = 15;
    private const int BottomMargin = 56;

    public static async Task<string> ExportAsync(OrderReceiptPdfRequest request)
    {
        var fileName = $"SmartOrder_Pedido_{(request.Folio?.ToString(CultureInfo.InvariantCulture) ?? "Borrador")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SmartOrder", "Orders");
        Directory.CreateDirectory(exportDirectory);

        var filePath = Path.Combine(exportDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, BuildPdf(request));
        return filePath;
    }

    private static byte[] BuildPdf(OrderReceiptPdfRequest request)
    {
        var pages = BuildPages(request);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>"
        };

        var kids = string.Join(" ", Enumerable.Range(0, pages.Count).Select(index => $"{4 + (index * 2)} 0 R"));
        objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");

        for (var index = 0; index < pages.Count; index++)
        {
            var pageObjectNumber = 4 + (index * 2);
            var contentObjectNumber = pageObjectNumber + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber} 0 R >>");

            var content = pages[index];
            var contentBytes = Encoding.ASCII.GetBytes(content);
            objects.Add($"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream");
        }

        return WritePdf(objects);
    }

    private static List<string> BuildPages(OrderReceiptPdfRequest request)
    {
        var pages = new List<string>();
        var content = new StringBuilder();
        var y = PageHeight - Margin;

        void NewPage()
        {
            if (content.Length > 0)
            {
                pages.Add(content.ToString());
                content.Clear();
            }

            y = PageHeight - Margin;
        }

        void EnsureSpace(int lines = 1)
        {
            if (y - (lines * LineHeight) < BottomMargin)
            {
                NewPage();
                AddHeader(content, request, ref y, includeDetails: false);
                AddTableHeader(content, ref y);
            }
        }

        NewPage();
        AddHeader(content, request, ref y, includeDetails: true);
        AddTableHeader(content, ref y);

        foreach (var item in request.Items)
        {
            var wrappedCategory = Wrap(item.CategoryName, 15).ToList();
            var wrappedProduct = Wrap(item.ProductName, 24).ToList();
            var rowLines = Math.Max(1, Math.Max(wrappedCategory.Count, wrappedProduct.Count));
            EnsureSpace(rowLines + 1);

            for (var lineIndex = 0; lineIndex < rowLines; lineIndex++)
            {
                var categoryText = lineIndex < wrappedCategory.Count ? wrappedCategory[lineIndex] : string.Empty;
                var productText = lineIndex < wrappedProduct.Count ? wrappedProduct[lineIndex] : string.Empty;
                DrawText(content, 46, y, categoryText, 9);
                DrawText(content, 132, y, productText, 9);

                if (lineIndex == 0)
                {
                    var unitPrice = request.IsInternalProduction ? 0 : item.Price;
                    var discountAmount = request.IsInternalProduction ? 0 : item.DiscountAmount;
                    var lineTotal = request.IsInternalProduction ? 0 : item.Subtotal - item.DiscountAmount;

                    DrawText(content, 302, y, item.Quantity.ToString(CultureInfo.InvariantCulture), 9);
                    DrawText(content, 348, y, Money(unitPrice), 9);
                    DrawText(content, 426, y, Money(discountAmount), 9);
                    DrawText(content, 504, y, Money(lineTotal), 9);
                }

                y -= LineHeight;
            }
        }

        DrawLine(content, 46, y + 6, 566, y + 6, "D9E2EC");
        EnsureSpace(7);
        y -= 12;
        DrawFilledRectangle(content, 354, y - 38, 212, 54, "E8F5EF");
        DrawText(content, 372, y, "Subtotal:", 10, "102A43");
        DrawText(content, 466, y, Money(request.Subtotal), 10, "102A43");
        y -= LineHeight;
        DrawText(content, 372, y, "Descuento:", 10, "7C5E10");
        DrawText(content, 466, y, Money(request.Discount), 10, "7C5E10");
        y -= LineHeight;
        DrawText(content, 372, y, "Total:", 12, "0B6E4F");
        DrawText(content, 466, y, Money(request.Total), 12, "0B6E4F");

        pages.Add(content.ToString());
        return pages;
    }

    private static void AddHeader(StringBuilder content, OrderReceiptPdfRequest request, ref int y, bool includeDetails)
    {
        DrawFilledRectangle(content, 36, y - 52, 540, 66, "EAF2FB");
        DrawText(content, 46, y, request.BranchName, 18, "12344D");
        y -= 16;

        if (!string.IsNullOrWhiteSpace(request.BranchAddress))
        {
            DrawText(content, 46, y, request.BranchAddress, 8, "486581");
            y -= 11;
        }

        if (!string.IsNullOrWhiteSpace(request.BranchPhone))
        {
            DrawText(content, 46, y, $"Tel. {request.BranchPhone}", 8, "486581");
            y -= 11;
        }

        y -= 14;

        if (includeDetails)
        {
            DrawFilledRectangle(content, 46, y - 28, 520, 40, "FFF4D6");
            DrawText(content, 46, y, $"Folio: {(request.Folio?.ToString(CultureInfo.InvariantCulture) ?? "Borrador")}", 10, "102A43");
            DrawText(content, 330, y, $"Entrega: {request.DeliveryDate:dd/MM/yyyy} {DateTime.Today.Add(request.DeliveryTime):HH:mm}", 10, "102A43");
            y -= LineHeight;
            DrawText(content, 46, y, request.IsInternalProduction ? "Tipo: Produccion del local" : $"Cliente: {request.CustomerName}", 10, "102A43");
            DrawText(content, 330, y, $"Piezas: {request.Items.Sum(item => item.Quantity).ToString(CultureInfo.InvariantCulture)}", 10, "102A43");
            y -= 24;
        }
    }

    private static void AddTableHeader(StringBuilder content, ref int y)
    {
        DrawFilledRectangle(content, 46, y - 13, 520, 24, "EAF2FB");
        DrawText(content, 46, y, "Categoria", 9, "12344D");
        DrawText(content, 132, y, "Producto", 9, "12344D");
        DrawText(content, 302, y, "Cant.", 9, "12344D");
        DrawText(content, 348, y, "Precio", 9, "12344D");
        DrawText(content, 426, y, "Descuento", 9, "12344D");
        DrawText(content, 504, y, "Total", 9, "12344D");
        y -= 14;
        DrawLine(content, 46, y + 2, 566, y + 2, "B7DBC9");
        y -= 10;
    }

    private static byte[] WritePdf(IReadOnlyList<string> objects)
    {
        var output = new MemoryStream();
        var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        var offsets = new List<long> { 0 };

        writer.Write("%PDF-1.4\n");
        writer.Flush();

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            writer.Write($"{index + 1} 0 obj\n");
            writer.Write(objects[index]);
            writer.Write("\nendobj\n");
            writer.Flush();
        }

        var xrefOffset = output.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            writer.Write($"{offset:0000000000} 00000 n \n");
        }

        writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        writer.Flush();
        return output.ToArray();
    }

    private static void DrawText(StringBuilder content, int x, int y, string text, int fontSize, string colorHex = "102A43")
    {
        var (red, green, blue) = ToRgb(colorHex);
        content.AppendLine($"q {red} {green} {blue} rg BT /F1 {fontSize} Tf {x} {y} Td <{ToPdfHexText(text)}> Tj ET Q");
    }

    private static void DrawLine(StringBuilder content, int x1, int y1, int x2, int y2, string colorHex = "D9E2EC")
    {
        var (red, green, blue) = ToRgb(colorHex);
        content.AppendLine($"q {red} {green} {blue} RG 0.5 w {x1} {y1} m {x2} {y2} l S Q");
    }

    private static void DrawFilledRectangle(StringBuilder content, int x, int y, int width, int height, string colorHex)
    {
        var (red, green, blue) = ToRgb(colorHex);
        content.AppendLine($"q {red} {green} {blue} rg {x} {y} {width} {height} re f Q");
    }

    private static string Money(decimal value)
    {
        return value.ToString("$#,##0.00", CultureInfo.InvariantCulture);
    }

    private static IEnumerable<string> Wrap(string value, int maxLength)
    {
        var words = NormalizePdfText(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

    private static string NormalizePdfText(string value)
    {
        return string.Join(' ', (value ?? string.Empty).Trim().Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ToPdfHexText(string value)
    {
        var normalized = NormalizePdfText(value);
        var bytes = normalized.Select(character => character <= 255 ? (byte)character : (byte)'?');
        return string.Concat(bytes.Select(currentByte => currentByte.ToString("X2", CultureInfo.InvariantCulture)));
    }

    private static (string Red, string Green, string Blue) ToRgb(string hex)
    {
        var normalizedHex = hex.TrimStart('#');
        var red = int.Parse(normalizedHex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255m;
        var green = int.Parse(normalizedHex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255m;
        var blue = int.Parse(normalizedHex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255m;

        return (FormatColor(red), FormatColor(green), FormatColor(blue));
    }

    private static string FormatColor(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
