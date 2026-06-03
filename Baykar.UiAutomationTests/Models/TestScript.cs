namespace Baykar.UiAutomationTests.Models;

public sealed class TestScript
{
    public string TestName { get; init; } = string.Empty;

    public List<TestStep> Steps { get; init; } = [];
}
