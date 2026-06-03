namespace Baykar.UiAutomationTests.Models;

public sealed class TestStep
{
    public string Name { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string? AutomationId { get; init; }

    public string? TextBoxAutomationId { get; init; }

    public string? ButtonAutomationId { get; init; }

    public string? Value { get; init; }

    public string? ExpectedTextAutomationId { get; init; }

    public string? ExpectedContains { get; init; }

    public double? ExpectedGreaterThan { get; init; }

    public int TimeoutMs { get; init; } = 5000;
}
