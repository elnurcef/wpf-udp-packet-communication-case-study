namespace Baykar.UiAutomationTests.Results;

public sealed class TestStepResult
{
    public string StepName { get; init; } = string.Empty;

    public bool IsPassed { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime StartedAt { get; init; }

    public DateTime FinishedAt { get; init; }
}
