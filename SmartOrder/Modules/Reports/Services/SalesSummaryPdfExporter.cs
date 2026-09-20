using System.Globalization;
using System.Text;

namespace SmartOrder.Modules.Reports.Services;

public sealed record SalesSummaryPdfDay(
    DateTime Date,
    decimal TotalSalesAmount,
    decimal CashSalesAmount,
    decimal CardSalesAmount,
    int TotalItemsSold);

public sealed record SalesSummaryPdfRequest(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalSalesAmount,
    decimal CashSalesAmount,
    decimal CardSalesAmount,
    int TotalItemsSold,
    IReadOnlyList<SalesSummaryPdfDay> Days);

public static class SalesSummaryPdfExporter
{
    private const int PageWidth = 612;
    private const int PageHeight = 792;
    private const int Margin = 46;
    private const int BottomMargin = 56;
    private const int RowHeight = 22;

    public static async Task<string> ExportAsync(SalesSummaryPdfRequest request)
    {
        var fileName = $"SmartOrder_AcumuladoVentas_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf";
        var exportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SmartOrder",
            "Reports");
        Directory.CreateDirectory(exportDirectory);

        var filePath = Path.Combine(exportDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, BuildPdf(request));
        return filePath;
    }

    private static byte[] BuildPdf(SalesSummaryPdfRequest request)
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

    private static List<string> BuildPages(SalesSummaryPdfRequest request)
    {
        var pages = new List<string>();
        var content = new StringBuilder();
        var y = PageHeight - Margin;

        void FinishPage()
        {
            if (content.Length == 0)
            {
                return;
            }

            pages.Add(content.ToString());
            content.Clear();
        }

        void StartPage(bool includeSummary)
        {
            y = PageHeight - Margin;
            AddHeader(content, request, ref y, includeSummary);
            AddTableHeader(content, ref y);
        }

        StartPage(includeSummary: true);

        if (request.Days.Count == 0)
        {
            DrawText(content, 46, y - 8, "No hay ventas registradas para el rango seleccionado.", 11, "486581");
            FinishPage();
            return pages;
        }

        foreach (var day in request.Days.OrderBy(day => day.Date))
        {
            if (y - RowHeight < BottomMargin)
            {
                FinishPage();
                StartPage(includeSummary: false);
            }

            DrawText(content, 46, y, day.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 10, "486581");
            DrawText(content, 145, y, Money(day.TotalSalesAmount), 10, "6C4C1D");
            DrawText(content, 255, y, Money(day.CashSalesAmount), 10, "0B6E4F");
            DrawText(content, 365, y, Money(day.CardSalesAmount), 10, "1B587C");
            DrawText(content, 492, y, day.TotalItemsSold.ToString(CultureInfo.InvariantCulture), 10, "102A43");
            y -= RowHeight;
            DrawLine(content, 46, y + 8, 566, y + 8, "E6ECF2");
        }

        FinishPage();
        return pages;
    }

    private static void AddHeader(StringBuilder content, SalesSummaryPdfRequest request, ref int y, bool includeSummary)
    {
        DrawFilledRectangle(content, 36, y - 50, 540, 64, "12344D");
        DrawText(content, 46, y, "Acumulado de ventas", 19, "FFFFFF");
        y -= 20;
        DrawText(content, 46, y, $"Periodo: {request.StartDate:dd/MM/yyyy} al {request.EndDate:dd/MM/yyyy}", 10, "D9E2EC");
        DrawText(content, 402, y, $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", 8, "D9E2EC");
        y -= 38;

        if (!includeSummary)
        {
            return;
        }

        AddMetric(content, 46, y, 120, "Monto total", Money(request.TotalSalesAmount), "FFF4D6", "6C4C1D");
        AddMetric(content, 177, y, 120, "Efectivo", Money(request.CashSalesAmount), "E8F5EF", "0B6E4F");
        AddMetric(content, 308, y, 120, "Tarjeta", Money(request.CardSalesAmount), "EAF2FB", "1B587C");
        AddMetric(content, 439, y, 127, "Productos", request.TotalItemsSold.ToString(CultureInfo.InvariantCulture), "EAF2FB", "164A68");
        y -= 66;
    }

    private static void AddMetric(StringBuilder content, int x, int y, int width, string label, string value, string backgroundColor, string textColor)
    {
        DrawFilledRectangle(content, x, y - 42, width, 52, backgroundColor);
        DrawText(content, x + 10, y - 7, label, 8, "486581");
        DrawText(content, x + 10, y - 26, value, 12, textColor);
    }

    private static void AddTableHeader(StringBuilder content, ref int y)
    {
        DrawFilledRectangle(content, 46, y - 13, 520, 25, "EAF1F8");
        DrawText(content, 46, y, "Fecha", 9, "243B53");
        DrawText(content, 145, y, "Monto total", 9, "243B53");
        DrawText(content, 255, y, "Efectivo", 9, "243B53");
        DrawText(content, 365, y, "Tarjeta", 9, "243B53");
        DrawText(content, 480, y, "Productos", 9, "243B53");
        y -= 28;
    }

    private static byte[] WritePdf(IReadOnlyList<string> objects)
    {
        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
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

    private static void DrawText(StringBuilder content, int x, int y, string text, int fontSize, string colorHex)
    {
        var (red, green, blue) = ToRgb(colorHex);
        content.AppendLine($"q {red} {green} {blue} rg BT /F1 {fontSize} Tf {x} {y} Td <{ToPdfHexText(text)}> Tj ET Q");
    }

    private static void DrawLine(StringBuilder content, int x1, int y1, int x2, int y2, string colorHex)
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

    private static string ToPdfHexText(string value)
    {
        var normalized = string.Join(' ', (value ?? string.Empty).Trim().Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
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
