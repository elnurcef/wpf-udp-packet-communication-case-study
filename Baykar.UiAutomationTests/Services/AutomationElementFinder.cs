using FlaUI.Core.AutomationElements;
using FlaUI.Core.Patterns;

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
}

public sealed record TextWaitResult(bool IsFound, string ActualText);
