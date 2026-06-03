namespace Baykar.Shared.Matlab;

public sealed class MatlabConversionResult
{
    private MatlabConversionResult(bool isSuccess, string outputFilePath, string errorMessage)
    {
        IsSuccess = isSuccess;
        OutputFilePath = outputFilePath;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public string OutputFilePath { get; }

    public string ErrorMessage { get; }

    public static MatlabConversionResult Success(string outputFilePath)
    {
        return new MatlabConversionResult(true, outputFilePath, string.Empty);
    }

    public static MatlabConversionResult Fail(string errorMessage)
    {
        return new MatlabConversionResult(false, string.Empty, errorMessage);
    }
}
