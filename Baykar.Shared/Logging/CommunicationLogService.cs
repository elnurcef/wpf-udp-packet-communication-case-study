using System.Globalization;
using System.Text;
using Baykar.Shared.Enums;
using Baykar.Shared.Models;

namespace Baykar.Shared.Logging;

public sealed class CommunicationLogService
{
    private const string Header = "TimestampUtc,PacketType,Data1,Data2,Data3,Data4,Data5,Data6Float,Data7Float,SystemTime,Data6Double";
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private string? _currentLogFilePath;

    public string CreateNewLogFilePath()
    {
        string directoryPath = GetLogDirectoryPath();
        Directory.CreateDirectory(directoryPath);

        string fileName = $"CommunicationLog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(directoryPath, fileName);

        File.WriteAllText(filePath, Header + Environment.NewLine, Encoding.UTF8);
        _currentLogFilePath = filePath;

        return filePath;
    }

    public async Task AppendAsync(CommunicationLogRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        string filePath = _currentLogFilePath ?? CreateNewLogFilePath();

        await _writeLock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, Header + Environment.NewLine, Encoding.UTF8, cancellationToken);
            }

            await File.AppendAllTextAsync(filePath, ToCsvLine(record) + Environment.NewLine, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IReadOnlyList<CommunicationLogRecord>> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return [];
        }

        string[] lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);
        List<CommunicationLogRecord> records = [];

        foreach (string line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            records.Add(ParseCsvLine(line));
        }

        return records;
    }

    private static string GetLogDirectoryPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Release", "Log Kayıtları");
    }

    private static string ToCsvLine(CommunicationLogRecord record)
    {
        string[] values =
        [
            record.TimestampUtc.ToString("O", CultureInfo.InvariantCulture),
            record.PacketType.ToString(),
            FormatNullable(record.Data1),
            FormatNullable(record.Data2),
            FormatNullable(record.Data3),
            FormatNullable(record.Data4),
            FormatNullable(record.Data5),
            FormatNullable(record.Data6Float),
            FormatNullable(record.Data7Float),
            FormatNullable(record.SystemTime),
            FormatNullable(record.Data6Double)
        ];

        return string.Join(",", values);
    }

    private static CommunicationLogRecord ParseCsvLine(string line)
    {
        string[] values = line.Split(',');

        if (values.Length != 11)
        {
            throw new FormatException("CSV log row has an invalid column count.");
        }

        return new CommunicationLogRecord
        {
            TimestampUtc = DateTime.Parse(values[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            PacketType = Enum.Parse<PacketType>(values[1]),
            Data1 = ParseNullableByte(values[2]),
            Data2 = ParseNullableByte(values[3]),
            Data3 = ParseNullableShort(values[4]),
            Data4 = ParseNullableInt(values[5]),
            Data5 = ParseNullableShort(values[6]),
            Data6Float = ParseNullableFloat(values[7]),
            Data7Float = ParseNullableFloat(values[8]),
            SystemTime = ParseNullableUInt(values[9]),
            Data6Double = ParseNullableDouble(values[10])
        };
    }

    private static string FormatNullable<T>(T? value)
        where T : struct, IFormattable
    {
        return value.HasValue
            ? value.Value.ToString(null, CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static byte? ParseNullableByte(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : byte.Parse(value, CultureInfo.InvariantCulture);
    }

    private static short? ParseNullableShort(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : short.Parse(value, CultureInfo.InvariantCulture);
    }

    private static int? ParseNullableInt(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static float? ParseNullableFloat(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : float.Parse(value, CultureInfo.InvariantCulture);
    }

    private static uint? ParseNullableUInt(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : uint.Parse(value, CultureInfo.InvariantCulture);
    }

    private static double? ParseNullableDouble(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : double.Parse(value, CultureInfo.InvariantCulture);
    }
}
