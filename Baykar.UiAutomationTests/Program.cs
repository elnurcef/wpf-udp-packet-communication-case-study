using System.Diagnostics;
using Baykar.UiAutomationTests.Models;
using Baykar.UiAutomationTests.Reports;
using Baykar.UiAutomationTests.Results;
using Baykar.UiAutomationTests.Services;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

const string UserInterfaceProcessName = "Baykar.UserInterface";
const string UserInterfaceWindowTitle = "Baykar User Interface";

try
{
    string scriptPath = GetScriptPath();

    Console.WriteLine($"Script: {scriptPath}");
    Console.WriteLine("Attaching to Baykar.UserInterface...");

    using UIA3Automation automation = new();
    using Application application = AttachToUserInterface();

    Window mainWindow = application.GetMainWindow(automation, TimeSpan.FromSeconds(5))
        ?? throw new InvalidOperationException("Baykar.UserInterface main window was not found.");

    if (!mainWindow.Title.Contains(UserInterfaceWindowTitle, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"Attached window title: {mainWindow.Title}");
    }

    PrepareMainWindow(mainWindow);

    TestScriptLoader loader = new();
    TestScript script = await loader.LoadAsync(scriptPath, CancellationToken.None);

    UiAutomationTestRunner runner = new(new AutomationElementFinder());
    TestRunResult result = await runner.RunAsync(script, mainWindow, CancellationToken.None);

    PrintResult(result);
    await TryGeneratePdfReportAsync(result, CancellationToken.None);

    return result.IsPassed ? 0 : 1;
}
catch (Exception exception)
{
    Console.WriteLine($"[FAIL] {exception.Message}");
    Console.WriteLine("Final Result: FAILED");
    return 1;
}

static string GetScriptPath()
{
    string defaultScriptPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "Baykar.UserInterface",
        "Release",
        "Test",
        "default-test-script.json"));

    if (File.Exists(defaultScriptPath))
    {
        return defaultScriptPath;
    }

    Console.Write("Default test script was not found. Enter test script path: ");
    string? scriptPath = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(scriptPath))
    {
        throw new InvalidOperationException("Test script path is required.");
    }

    return scriptPath;
}

static Application AttachToUserInterface()
{
    Process? process = Process
        .GetProcessesByName(UserInterfaceProcessName)
        .OrderByDescending(candidate => candidate.StartTime)
        .FirstOrDefault(candidate => !candidate.HasExited);

    if (process is null)
    {
        throw new InvalidOperationException("Baykar.UserInterface is not running. Start it before running automation tests.");
    }

    return Application.Attach(process);
}

static void PrepareMainWindow(Window mainWindow)
{
    mainWindow.Focus();

    try
    {
        var windowPattern = mainWindow.Patterns.Window.PatternOrDefault;

        if (windowPattern is not null && windowPattern.CanMaximize)
        {
            windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
        }
    }
    catch
    {
    }

    Thread.Sleep(500);
}

static void PrintResult(TestRunResult result)
{
    Console.WriteLine();
    Console.WriteLine($"Test: {result.TestName}");

    foreach (TestStepResult stepResult in result.StepResults)
    {
        string status = stepResult.IsPassed ? "PASS" : "FAIL";
        string message = stepResult.IsPassed || string.IsNullOrWhiteSpace(stepResult.Message)
            ? string.Empty
            : $" - {stepResult.Message}";

        Console.WriteLine($"[{status}] {stepResult.StepName}{message}");
    }

    Console.WriteLine();
    Console.WriteLine($"Final Result: {(result.IsPassed ? "PASSED" : "FAILED")}");
}

static async Task TryGeneratePdfReportAsync(TestRunResult result, CancellationToken cancellationToken)
{
    try
    {
        TestReportService reportService = new();
        string reportPath = await reportService.GeneratePdfReportAsync(result, cancellationToken);
        Console.WriteLine($"PDF Report: {reportPath}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"PDF report generation failed: {exception.Message}");
    }
}
