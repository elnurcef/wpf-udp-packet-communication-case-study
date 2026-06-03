using Baykar.UiAutomationTests.Models;
using Baykar.UiAutomationTests.Results;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Patterns;
using FlaUI.Core.WindowsAPI;

namespace Baykar.UiAutomationTests.Services;

public sealed class UiAutomationTestRunner
{
    private readonly AutomationElementFinder _elementFinder;

    public UiAutomationTestRunner(AutomationElementFinder elementFinder)
    {
        _elementFinder = elementFinder;
    }

    public async Task<TestRunResult> RunAsync(
        TestScript script,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(mainWindow);

        DateTime startedAt = DateTime.UtcNow;
        List<TestStepResult> stepResults = [];

        foreach (TestStep step in script.Steps)
        {
            TestStepResult result = await ExecuteStepAsync(step, mainWindow, cancellationToken);
            stepResults.Add(result);
        }

        DateTime finishedAt = DateTime.UtcNow;

        return new TestRunResult
        {
            TestName = script.TestName,
            IsPassed = stepResults.All(result => result.IsPassed),
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            StepResults = stepResults
        };
    }

    private async Task<TestStepResult> ExecuteStepAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        DateTime startedAt = DateTime.UtcNow;

        try
        {
            string message = step.Action.Trim() switch
            {
                "click" => await ExecuteClickAsync(step, mainWindow, cancellationToken),
                "clickAndWaitForText" => await ExecuteClickAndWaitForTextAsync(step, mainWindow, cancellationToken),
                "setTextAndClick" => await ExecuteSetTextAndClickAsync(step, mainWindow, cancellationToken),
                "waitForText" => await ExecuteWaitForTextAsync(step, mainWindow, cancellationToken),
                "waitForTextContains" => await ExecuteWaitForTextAsync(step, mainWindow, cancellationToken),
                "waitForTextNotEmpty" => await ExecuteWaitForTextNotEmptyAsync(step, mainWindow, cancellationToken),
                "waitForNumericGreaterThan" => await ExecuteWaitForNumericGreaterThanAsync(step, mainWindow, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported action: {step.Action}")
            };

            return new TestStepResult
            {
                StepName = step.Name,
                IsPassed = true,
                Message = message,
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new TestStepResult
            {
                StepName = step.Name,
                IsPassed = false,
                Message = exception.Message,
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<string> ExecuteClickAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        string automationId = RequireValue(step.AutomationId, "AutomationId");
        AutomationElement element = await FindRequiredElementAsync(step, mainWindow, automationId, cancellationToken);

        InvokeElement(element);
        await VerifyExpectedTextAsync(step, mainWindow, cancellationToken);

        return "Step completed.";
    }

    private async Task<string> ExecuteClickAndWaitForTextAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        string automationId = RequireValue(step.AutomationId, "AutomationId");
        AutomationElement element = await FindRequiredElementAsync(step, mainWindow, automationId, cancellationToken);

        InvokeElement(element);
        await VerifyExpectedTextAsync(step, mainWindow, cancellationToken);

        return "Step completed.";
    }

    private async Task<string> ExecuteSetTextAndClickAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        string textBoxAutomationId = RequireValue(step.TextBoxAutomationId, "TextBoxAutomationId");
        string buttonAutomationId = RequireValue(step.ButtonAutomationId, "ButtonAutomationId");
        string value = step.Value ?? string.Empty;

        AutomationElement textBoxElement = await FindRequiredElementAsync(step, mainWindow, textBoxAutomationId, cancellationToken);
        SetTextBoxValue(textBoxElement, value);

        AutomationElement buttonElement = await FindRequiredElementAsync(step, mainWindow, buttonAutomationId, cancellationToken);
        InvokeElement(buttonElement);

        await VerifyExpectedTextAsync(step, mainWindow, cancellationToken);

        return "Step completed.";
    }

    private async Task<string> ExecuteWaitForTextAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        await VerifyExpectedTextAsync(step, mainWindow, cancellationToken);

        return "Expected text was found.";
    }

    private async Task<string> ExecuteWaitForTextNotEmptyAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        string automationId = RequireValue(step.ExpectedTextAutomationId, "ExpectedTextAutomationId");
        TextWaitResult result = await _elementFinder.WaitForTextNotEmptyAsync(
            mainWindow,
            automationId,
            step.TimeoutMs,
            cancellationToken);

        if (!result.IsFound)
        {
            throw new TimeoutException(CreateFailureMessage(
                step,
                automationId,
                "non-empty text",
                result.ActualText));
        }

        return "Text was not empty.";
    }

    private async Task<string> ExecuteWaitForNumericGreaterThanAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        string automationId = RequireValue(step.ExpectedTextAutomationId, "ExpectedTextAutomationId");
        double expectedGreaterThan = step.ExpectedGreaterThan
            ?? throw new InvalidOperationException("ExpectedGreaterThan is required.");

        NumericWaitResult result = await _elementFinder.WaitForNumericGreaterThanAsync(
            mainWindow,
            automationId,
            expectedGreaterThan,
            step.TimeoutMs,
            cancellationToken);

        if (!result.IsFound)
        {
            string actualValue = result.ActualValue.HasValue
                ? result.ActualValue.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture)
                : $"not numeric; text='{result.ActualText}'";

            throw new TimeoutException(CreateFailureMessage(
                step,
                automationId,
                $"> {expectedGreaterThan.ToString("G", System.Globalization.CultureInfo.InvariantCulture)}",
                actualValue));
        }

        return "Numeric value was greater than expected.";
    }

    private async Task VerifyExpectedTextAsync(
        TestStep step,
        Window mainWindow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.ExpectedTextAutomationId))
        {
            return;
        }

        string expectedContains = RequireValue(step.ExpectedContains, "ExpectedContains");
        TextWaitResult result = await _elementFinder.WaitForTextContainsAsync(
            mainWindow,
            step.ExpectedTextAutomationId,
            expectedContains,
            step.TimeoutMs,
            cancellationToken);

        if (!result.IsFound)
        {
            throw new TimeoutException(CreateFailureMessage(
                step,
                step.ExpectedTextAutomationId,
                $"contains '{expectedContains}'",
                result.ActualText));
        }
    }

    private static void InvokeElement(AutomationElement element)
    {
        try
        {
            IInvokePattern? invokePattern = element.Patterns.Invoke.PatternOrDefault;

            if (invokePattern is not null)
            {
                invokePattern.Invoke();
                return;
            }
        }
        catch
        {
        }

        element.Click();
    }

    private static void SetTextBoxValue(AutomationElement element, string value)
    {
        try
        {
            IValuePattern? valuePattern = element.Patterns.Value.PatternOrDefault;

            if (valuePattern is not null && !valuePattern.IsReadOnly)
            {
                valuePattern.SetValue(string.Empty);
                valuePattern.SetValue(value);
                return;
            }
        }
        catch
        {
        }

        element.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Press(VirtualKeyShort.BACK);
        Keyboard.Type(value);
    }

    private async Task<AutomationElement> FindRequiredElementAsync(
        TestStep step,
        Window mainWindow,
        string automationId,
        CancellationToken cancellationToken)
    {
        AutomationElement? element = await _elementFinder.FindByAutomationIdAsync(
            mainWindow,
            automationId,
            step.TimeoutMs,
            cancellationToken);

        if (element is null)
        {
            throw new TimeoutException(CreateFailureMessage(
                step,
                automationId,
                "element to exist",
                "not found"));
        }

        return element;
    }

    private static string RequireValue(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} is required.");
        }

        return value;
    }

    private static string CreateFailureMessage(
        TestStep step,
        string automationId,
        string expectedValue,
        string actualValue)
    {
        int timeoutMs = step.TimeoutMs > 0 ? step.TimeoutMs : 5000;

        return $"Step='{step.Name}', AutomationId='{automationId}', Expected='{expectedValue}', Actual='{actualValue}', TimeoutMs={timeoutMs}.";
    }
}
