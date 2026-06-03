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
                "setTextAndClick" => await ExecuteSetTextAndClickAsync(step, mainWindow, cancellationToken),
                "waitForText" => await ExecuteWaitForTextAsync(step, mainWindow, cancellationToken),
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
        AutomationElement element = await FindRequiredElementAsync(mainWindow, automationId, step.TimeoutMs, cancellationToken);

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

        AutomationElement textBoxElement = await FindRequiredElementAsync(mainWindow, textBoxAutomationId, step.TimeoutMs, cancellationToken);
        SetTextBoxValue(textBoxElement, value);

        AutomationElement buttonElement = await FindRequiredElementAsync(mainWindow, buttonAutomationId, step.TimeoutMs, cancellationToken);
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
            throw new TimeoutException(
                $"Expected text was not found. AutomationId='{step.ExpectedTextAutomationId}', ExpectedContains='{expectedContains}', ActualText='{result.ActualText}'.");
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
        Window mainWindow,
        string automationId,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        AutomationElement? element = await _elementFinder.FindByAutomationIdAsync(
            mainWindow,
            automationId,
            timeoutMs,
            cancellationToken);

        if (element is null)
        {
            throw new TimeoutException($"Element was not found: {automationId}");
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
}
