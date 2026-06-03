using System.Diagnostics;
using System.Text;
using Baykar.UiAutomationTests.Models;
using Baykar.UiAutomationTests.Reports;
using Baykar.UiAutomationTests.Results;
using Baykar.UiAutomationTests.Services;
using Baykar.Shared.Localization;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

const string UserInterfaceProcessName = "Baykar.UserInterface";
const string UserInterfaceWindowTitle = "Baykar User Interface";

Console.OutputEncoding = Encoding.UTF8;
LocalizationService localizationService = new(GetOutputLanguage(args));

try
{
    string scriptPath = GetScriptPath(localizationService);

    Console.WriteLine($"{localizationService.GetText("Automation.Script")}: {scriptPath}");
    Console.WriteLine(localizationService.GetText("Automation.Attaching"));

    using UIA3Automation automation = new();
    using Application application = AttachToUserInterface(localizationService);

    Window mainWindow = application.GetMainWindow(automation, TimeSpan.FromSeconds(5))
        ?? throw new InvalidOperationException(localizationService.GetText("Automation.MainWindowNotFound"));

    if (!mainWindow.Title.Contains(UserInterfaceWindowTitle, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(localizationService.FormatText("Automation.AttachedWindowTitle", mainWindow.Title));
    }

    PrepareMainWindow(mainWindow);

    TestScriptLoader loader = new();
    TestScript script = await loader.LoadAsync(scriptPath, CancellationToken.None);

    UiAutomationTestRunner runner = new(new AutomationElementFinder(), localizationService);
    TestRunResult result = await runner.RunAsync(script, mainWindow, CancellationToken.None);

    PrintResult(result, localizationService);
    await TryGeneratePdfReportAsync(result, localizationService, CancellationToken.None);

    return result.IsPassed ? 0 : 1;
}
catch (Exception exception)
{
    Console.WriteLine($"[{localizationService.GetText("Automation.Fail")}] {exception.Message}");
    Console.WriteLine($"{localizationService.GetText("Automation.FinalResult")}: {localizationService.GetText("Automation.Failed")}");
    return 1;
}

static SupportedLanguage GetOutputLanguage(string[] args)
{
    for (int index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], "--lang", StringComparison.OrdinalIgnoreCase))
        {
            return LocalizationService.FromCode(args[index + 1]);
        }
    }

    return SupportedLanguage.English;
}

static string GetScriptPath(LocalizationService localizationService)
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

    Console.Write(localizationService.GetText("Automation.DefaultScriptNotFoundPrompt"));
    string? scriptPath = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(scriptPath))
    {
        throw new InvalidOperationException(localizationService.GetText("Automation.ScriptPathRequired"));
    }

    return scriptPath;
}

static Application AttachToUserInterface(LocalizationService localizationService)
{
    Process? process = Process
        .GetProcessesByName(UserInterfaceProcessName)
        .OrderByDescending(candidate => candidate.StartTime)
        .FirstOrDefault(candidate => !candidate.HasExited);

    if (process is null)
    {
        throw new InvalidOperationException(localizationService.GetText("Automation.UserInterfaceNotRunning"));
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

static void PrintResult(TestRunResult result, LocalizationService localizationService)
{
    Console.WriteLine();
    Console.WriteLine($"{localizationService.GetText("Automation.Test")}: {GetDisplayName(result.TestName, localizationService)}");

    foreach (TestStepResult stepResult in result.StepResults)
    {
        string status = stepResult.IsPassed
            ? localizationService.GetText("Automation.Pass")
            : localizationService.GetText("Automation.Fail");

        string message = stepResult.IsPassed || string.IsNullOrWhiteSpace(stepResult.Message)
            ? string.Empty
            : $" - {stepResult.Message}";

        Console.WriteLine($"[{status}] {GetDisplayName(stepResult.StepName, localizationService)}{message}");
    }

    Console.WriteLine();
    string finalResult = result.IsPassed
        ? localizationService.GetText("Automation.Passed")
        : localizationService.GetText("Automation.Failed");

    Console.WriteLine($"{localizationService.GetText("Automation.FinalResult")}: {finalResult}");
}

static async Task TryGeneratePdfReportAsync(
    TestRunResult result,
    LocalizationService localizationService,
    CancellationToken cancellationToken)
{
    try
    {
        TestReportService reportService = new(localizationService);
        string reportPath = await reportService.GeneratePdfReportAsync(result, cancellationToken);
        Console.WriteLine($"{localizationService.GetText("Automation.PdfReport")}: {reportPath}");
    }
    catch (Exception exception)
    {
        Console.WriteLine(localizationService.FormatText("Automation.PdfReportFailed", exception.Message));
    }
}

static string GetDisplayName(string name, LocalizationService localizationService)
{
    return localizationService.GetTextOrDefault($"AutomationStep.{name}", name);
}
