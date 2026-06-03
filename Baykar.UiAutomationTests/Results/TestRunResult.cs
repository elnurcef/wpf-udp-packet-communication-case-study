namespace Baykar.UiAutomationTests.Results;

public sealed class TestRunResult
{
    public string TestName { get; init; } = string.Empty;

    public bool IsPassed { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime FinishedAt { get; init; }

    public List<TestStepResult> StepResults { get; init; } = [];
}
