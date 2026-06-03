using System.Text.Json;
using Baykar.UiAutomationTests.Models;

namespace Baykar.UiAutomationTests.Services;

public sealed class TestScriptLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TestScript> LoadAsync(string filePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Test script file was not found.", filePath);
        }

        await using FileStream fileStream = File.OpenRead(filePath);
        TestScript? script = await JsonSerializer.DeserializeAsync<TestScript>(fileStream, JsonOptions, cancellationToken);

        if (script is null)
        {
            throw new InvalidOperationException("Test script could not be read.");
        }

        if (string.IsNullOrWhiteSpace(script.TestName))
        {
            throw new InvalidOperationException("Test script name is required.");
        }

        if (script.Steps.Count == 0)
        {
            throw new InvalidOperationException("Test script must contain at least one step.");
        }

        return script;
    }
}
