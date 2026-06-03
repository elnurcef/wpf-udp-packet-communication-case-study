using FlaUI.Core.AutomationElements;
using FlaUI.Core.Patterns;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Baykar.UiAutomationTests.Services;

public sealed class AutomationElementFinder
{
    private const int PollIntervalMs = 100;

    public async Task<AutomationElement?> FindByAutomationIdAsync(
        AutomationElement rootElement,
        string automationId,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(GetEffectiveTimeout(timeoutMs));

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AutomationElement? element = rootElement.FindFirstDescendant(automationId);

            if (element is not null)
            {
                return element;
            }

            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        return null;
    }

    public async Task<TextWaitResult> WaitForTextContainsAsync(
        AutomationElement rootElement,
        string automationId,
        string expectedText,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(expectedText);

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(GetEffectiveTimeout(timeoutMs));
        string actualText = string.Empty;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AutomationElement? element = rootElement.FindFirstDescendant(automationId);

            if (element is not null)
            {
                actualText = GetElementText(element);

                if (actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                {
                    return new TextWaitResult(true, actualText);
                }
            }

            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        return new TextWaitResult(false, actualText);
    }

    public async Task<TextWaitResult> WaitForAnyTextContainsAsync(
        AutomationElement rootElement,
        string automationId,
        IReadOnlyList<string> expectedTexts,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(expectedTexts);

        string[] normalizedExpectedTexts = expectedTexts
            .Where(expectedText => !string.IsNullOrWhiteSpace(expectedText))
            .ToArray();

        if (normalizedExpectedTexts.Length == 0)
        {
            throw new InvalidOperationException("At least one expected text value is required.");
        }

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(GetEffectiveTimeout(timeoutMs));
        string actualText = string.Empty;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AutomationElement? element = rootElement.FindFirstDescendant(automationId);

            if (element is not null)
            {
                actualText = GetElementText(element);

                if (normalizedExpectedTexts.Any(expectedText =>
                    actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase)))
                {
                    return new TextWaitResult(true, actualText);
                }
            }

            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        return new TextWaitResult(false, actualText);
    }

    public async Task<TextWaitResult> WaitForTextNotEmptyAsync(
        AutomationElement rootElement,
        string automationId,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(GetEffectiveTimeout(timeoutMs));
        string actualText = string.Empty;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AutomationElement? element = rootElement.FindFirstDescendant(automationId);

            if (element is not null)
            {
                actualText = GetElementText(element);

                if (!string.IsNullOrWhiteSpace(actualText))
                {
                    return new TextWaitResult(true, actualText);
                }
            }

            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        return new TextWaitResult(false, actualText);
    }

    public async Task<NumericWaitResult> WaitForNumericGreaterThanAsync(
        AutomationElement rootElement,
        string automationId,
        double expectedGreaterThan,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);

        DateTime deadline = DateTime.UtcNow.AddMilliseconds(GetEffectiveTimeout(timeoutMs));
        string actualText = string.Empty;
        double? actualValue = null;

        while (DateTime.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AutomationElement? element = rootElement.FindFirstDescendant(automationId);

            if (element is not null)
            {
                actualText = GetElementText(element);
                actualValue = TryParseFirstNumber(actualText);

                if (actualValue.HasValue && actualValue.Value > expectedGreaterThan)
                {
                    return new NumericWaitResult(true, actualText, actualValue);
                }
            }

            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        return new NumericWaitResult(false, actualText, actualValue);
    }

    public static string GetElementText(AutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        string valuePatternText = TryGetValuePatternText(element);

        if (!string.IsNullOrWhiteSpace(valuePatternText))
        {
            return valuePatternText;
        }

        string textPatternText = TryGetTextPatternText(element);

        if (!string.IsNullOrWhiteSpace(textPatternText))
        {
            return textPatternText;
        }

        return element.Name ?? string.Empty;
    }

    private static string TryGetValuePatternText(AutomationElement element)
    {
        try
        {
            IValuePattern? valuePattern = element.Patterns.Value.PatternOrDefault;
            return valuePattern?.Value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TryGetTextPatternText(AutomationElement element)
    {
        try
        {
            ITextPattern? textPattern = element.Patterns.Text.PatternOrDefault;
            return textPattern?.DocumentRange.GetText(-1) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int GetEffectiveTimeout(int timeoutMs)
    {
        return timeoutMs > 0 ? timeoutMs : 5000;
    }

    private static double? TryParseFirstNumber(string text)
    {
        Match match = Regex.Match(text, @"[-+]?\d+(\.\d+)?");

        if (!match.Success)
        {
            return null;
        }

        return double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : null;
    }
}

public sealed record TextWaitResult(bool IsFound, string ActualText);

public sealed record NumericWaitResult(bool IsFound, string ActualText, double? ActualValue);
