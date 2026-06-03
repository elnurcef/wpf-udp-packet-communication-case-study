using System.Globalization;
using System.Text;
using Baykar.UiAutomationTests.Results;
using Baykar.Shared.Localization;

namespace Baykar.UiAutomationTests.Reports;

public sealed class TestReportService
{
    private const int PageWidth = 595;
    private const int PageHeight = 842;
    private const int Margin = 40;
    private const int LineHeight = 18;
    private const int SmallLineHeight = 14;
    private readonly LocalizationService _localizationService;

    public TestReportService(LocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public async Task<string> GeneratePdfReportAsync(TestRunResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        string outputDirectoryPath = Path.Combine(AppContext.BaseDirectory, "Release", "TestReports");
        Directory.CreateDirectory(outputDirectoryPath);

        string outputFilePath = Path.Combine(
            outputDirectoryPath,
            $"test-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

        byte[] pdfBytes = BuildPdf(result, cancellationToken);
        await File.WriteAllBytesAsync(outputFilePath, pdfBytes, cancellationToken);

        return outputFilePath;
    }

    private byte[] BuildPdf(TestRunResult result, CancellationToken cancellationToken)
    {
        List<string> pageStreams = BuildPageStreams(result, cancellationToken);
        return BuildPdfDocument(pageStreams);
    }

    private List<string> BuildPageStreams(TestRunResult result, CancellationToken cancellationToken)
    {
        List<StringBuilder> pages = [];
        StringBuilder currentPage = AddPage(pages);
        double y = Margin;

        DrawText(currentPage, Text("Report.Title"), 20, true, Margin, y);
        y += 28;

        DrawText(currentPage, $"{Text("Report.TestName")}: {GetDisplayName(result.TestName)}", 10, false, Margin, y);
        y += LineHeight;
        DrawText(currentPage, $"{Text("Report.StartTime")}: {FormatDateTime(result.StartedAt)}", 10, false, Margin, y);
        y += LineHeight;
        DrawText(currentPage, $"{Text("Report.FinishTime")}: {FormatDateTime(result.FinishedAt)}", 10, false, Margin, y);
        y += LineHeight;
        DrawText(currentPage, $"{Text("Report.Duration")}: {FormatDuration(result.FinishedAt - result.StartedAt)}", 10, false, Margin, y);
        y += LineHeight;
        DrawText(currentPage, $"{Text("Report.FinalResult")}: {GetResultText(result.IsPassed)}", 12, true, Margin, y);
        y += LineHeight + 10;

        DrawText(currentPage, Text("Report.StepResults"), 12, true, Margin, y);
        y += LineHeight;
        DrawTableHeader(currentPage, ref y);

        foreach (TestStepResult stepResult in result.StepResults)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double rowHeight = GetStepRowHeight(stepResult);

            if (y + rowHeight > PageHeight - Margin)
            {
                currentPage = AddPage(pages);
                y = Margin;
                DrawTableHeader(currentPage, ref y);
            }

            DrawStepRow(currentPage, stepResult, ref y);
        }

        return pages.Select(page => page.ToString()).ToList();
    }

    private static StringBuilder AddPage(List<StringBuilder> pages)
    {
        StringBuilder page = new();
        pages.Add(page);
        return page;
    }

    private void DrawTableHeader(StringBuilder page, ref double y)
    {
        DrawText(page, Text("Report.Step"), 10, true, Margin, y);
        DrawText(page, Text("Report.Result"), 10, true, Margin + 200, y);
        DrawText(page, Text("Report.Message"), 10, true, Margin + 280, y);
        y += LineHeight;
        DrawLine(page, Margin, y, PageWidth - Margin, y);
        y += 6;
    }

    private void DrawStepRow(StringBuilder page, TestStepResult stepResult, ref double y)
    {
        string resultText = GetResultText(stepResult.IsPassed);
        string message = string.IsNullOrWhiteSpace(stepResult.Message) ? "-" : stepResult.Message;
        string[] stepNameLines = WrapText(GetDisplayName(stepResult.StepName), 28).ToArray();
        string[] messageLines = WrapText(message, 58).ToArray();
        int lineCount = Math.Max(stepNameLines.Length, messageLines.Length);

        for (int index = 0; index < lineCount; index++)
        {
            string stepText = index < stepNameLines.Length ? stepNameLines[index] : string.Empty;
            string messageText = index < messageLines.Length ? messageLines[index] : string.Empty;

            DrawText(page, stepText, 8, false, Margin, y);

            if (index == 0)
            {
                DrawText(page, resultText, 8, false, Margin + 200, y);
            }

            DrawText(page, messageText, 8, false, Margin + 280, y);
            y += SmallLineHeight;
        }

        y += 6;
    }

    private static void DrawText(StringBuilder page, string text, int fontSize, bool isBold, double x, double y)
    {
        string fontName = isBold ? "F2" : "F1";
        page.AppendLine(FormattableString.Invariant(
            $"BT /{fontName} {fontSize} Tf {x:0.##} {ToPdfY(y):0.##} Td ({EscapePdfText(text)}) Tj ET"));
    }

    private static void DrawLine(StringBuilder page, double x1, double y1, double x2, double y2)
    {
        page.AppendLine(FormattableString.Invariant(
            $"0.5 w {x1:0.##} {ToPdfY(y1):0.##} m {x2:0.##} {ToPdfY(y2):0.##} l S"));
    }

    private double GetStepRowHeight(TestStepResult stepResult)
    {
        string message = string.IsNullOrWhiteSpace(stepResult.Message) ? "-" : stepResult.Message;
        int stepNameLineCount = WrapText(GetDisplayName(stepResult.StepName), 28).Count();
        int messageLineCount = WrapText(message, 58).Count();
        return (Math.Max(stepNameLineCount, messageLineCount) * SmallLineHeight) + 6;
    }

    private static byte[] BuildPdfDocument(IReadOnlyList<string> pageStreams)
    {
        int pageCount = pageStreams.Count;
        int firstPageObjectId = 6;
        int firstContentObjectId = firstPageObjectId + pageCount;
        int objectCount = firstContentObjectId + pageCount - 1;

        Dictionary<int, string> objects = new()
        {
            [1] = "<< /Type /Catalog /Pages 2 0 R >>",
            [3] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding 5 0 R >>",
            [4] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding 5 0 R >>",
            [5] = "<< /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [128 /gbreve /Gbreve /scedilla /Scedilla /dotlessi /Idotaccent] >>"
        };

        string pageReferences = string.Join(
            " ",
            Enumerable.Range(firstPageObjectId, pageCount).Select(objectId => $"{objectId} 0 R"));

        objects[2] = $"<< /Type /Pages /Kids [{pageReferences}] /Count {pageCount} >>";

        for (int index = 0; index < pageCount; index++)
        {
            int pageObjectId = firstPageObjectId + index;
            int contentObjectId = firstContentObjectId + index;
            string stream = pageStreams[index];
            int streamLength = Encoding.ASCII.GetByteCount(stream);

            objects[pageObjectId] =
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] " +
                $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectId} 0 R >>";

            objects[contentObjectId] =
                $"<< /Length {streamLength} >>{Environment.NewLine}stream{Environment.NewLine}" +
                $"{stream}endstream";
        }

        StringBuilder pdf = new();
        long[] offsets = new long[objectCount + 1];

        pdf.AppendLine("%PDF-1.4");

        for (int objectId = 1; objectId <= objectCount; objectId++)
        {
            offsets[objectId] = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.AppendLine($"{objectId} 0 obj");
            pdf.AppendLine(objects[objectId]);
            pdf.AppendLine("endobj");
        }

        long xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());

        pdf.AppendLine("xref");
        pdf.AppendLine($"0 {objectCount + 1}");
        pdf.AppendLine("0000000000 65535 f ");

        for (int objectId = 1; objectId <= objectCount; objectId++)
        {
            pdf.AppendLine($"{offsets[objectId]:D10} 00000 n ");
        }

        pdf.AppendLine("trailer");
        pdf.AppendLine($"<< /Size {objectCount + 1} /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        pdf.Append("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static IEnumerable<string> WrapText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return string.Empty;
            yield break;
        }

        string remainingText = text.Trim();

        while (remainingText.Length > maxLength)
        {
            int splitIndex = remainingText.LastIndexOf(' ', maxLength);

            if (splitIndex <= 0)
            {
                splitIndex = maxLength;
            }

            yield return remainingText[..splitIndex].Trim();
            remainingText = remainingText[splitIndex..].Trim();
        }

        yield return remainingText;
    }

    private static string EscapePdfText(string text)
    {
        StringBuilder escapedText = new();

        foreach (char character in text)
        {
            switch (character)
            {
                case '\\':
                    escapedText.Append(@"\\");
                    break;
                case '(':
                    escapedText.Append(@"\(");
                    break;
                case ')':
                    escapedText.Append(@"\)");
                    break;
                case 'ğ':
                    AppendOctal(escapedText, 128);
                    break;
                case 'Ğ':
                    AppendOctal(escapedText, 129);
                    break;
                case 'ş':
                    AppendOctal(escapedText, 130);
                    break;
                case 'Ş':
                    AppendOctal(escapedText, 131);
                    break;
                case 'ı':
                    AppendOctal(escapedText, 132);
                    break;
                case 'İ':
                    AppendOctal(escapedText, 133);
                    break;
                case 'Ç':
                case 'Ö':
                case 'Ü':
                case 'ç':
                case 'ö':
                case 'ü':
                    AppendOctal(escapedText, GetWindows1254Byte(character));
                    break;
                case >= ' ' and <= '~':
                    escapedText.Append(character);
                    break;
                default:
                    escapedText.Append('?');
                    break;
            }
        }

        return escapedText.ToString();
    }

    private static void AppendOctal(StringBuilder builder, int value)
    {
        builder.Append('\\');
        builder.Append(Convert.ToString(value, 8).PadLeft(3, '0'));
    }

    private static int GetWindows1254Byte(char character)
    {
        return character switch
        {
            'Ç' => 199,
            'Ö' => 214,
            'Ü' => 220,
            'ç' => 231,
            'ö' => 246,
            'ü' => 252,
            _ => '?'
        };
    }

    private static double ToPdfY(double topY)
    {
        return PageHeight - topY;
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    private static string FormatDuration(TimeSpan value)
    {
        return value.ToString(@"hh\:mm\:ss\.fff");
    }

    private string GetDisplayName(string name)
    {
        return _localizationService.GetTextOrDefault($"AutomationStep.{name}", name);
    }

    private string GetResultText(bool isPassed)
    {
        return isPassed
            ? Text("Report.Passed")
            : Text("Report.Failed");
    }

    private string Text(string key)
    {
        return _localizationService.GetText(key);
    }
}
