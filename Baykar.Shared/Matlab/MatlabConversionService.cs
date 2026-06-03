using System.Globalization;
using Baykar.Shared.Enums;
using Baykar.Shared.Logging;
using Baykar.Shared.Models;
using MatFileHandler;

namespace Baykar.Shared.Matlab;

public sealed class MatlabConversionService
{
    private static readonly string[] CommunicationPacket1Labels =
    [
        "Data1",
        "Data2",
        "Data3",
        "Data4",
        "Data5",
        "Data6",
        "Data7"
    ];

    private static readonly string[] CommunicationPacket2Labels =
    [
        "Data1",
        "Data2",
        "Data3",
        "Data4",
        "SystemTime",
        "Data6"
    ];

    private readonly CommunicationLogService _communicationLogService = new();

    public async Task<MatlabConversionResult> ConvertLogToMatAsync(
        string logFilePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            return MatlabConversionResult.Fail("Log file path is empty.");
        }

        if (!File.Exists(logFilePath))
        {
            return MatlabConversionResult.Fail("Log file does not exist.");
        }

        try
        {
            IReadOnlyList<CommunicationLogRecord> records = await _communicationLogService.ReadAsync(logFilePath, cancellationToken);

            List<CommunicationLogRecord> communicationPacket1Records = records
                .Where(record => record.PacketType == PacketType.CommunicationPacket1)
                .ToList();

            List<CommunicationLogRecord> communicationPacket2Records = records
                .Where(record => record.PacketType == PacketType.CommunicationPacket2)
                .ToList();

            string outputDirectoryPath = GetMatlabDirectoryPath();
            Directory.CreateDirectory(outputDirectoryPath);

            string outputFileName = $"{Path.GetFileNameWithoutExtension(logFilePath)}.mat";
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);

            await Task.Run(
                () => WriteMatFile(outputFilePath, communicationPacket1Records, communicationPacket2Records),
                cancellationToken);

            return MatlabConversionResult.Success(outputFilePath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return MatlabConversionResult.Fail(exception.Message);
        }
    }

    private static void WriteMatFile(
        string outputFilePath,
        IReadOnlyList<CommunicationLogRecord> communicationPacket1Records,
        IReadOnlyList<CommunicationLogRecord> communicationPacket2Records)
    {
        DataBuilder builder = new();

        IMatFile matFile = builder.NewFile(
        [
            builder.NewVariable(
                "communication_packet_1_values",
                CreateCommunicationPacket1Matrix(builder, communicationPacket1Records),
                false),
            builder.NewVariable(
                "communication_packet_1_labels",
                CreateLabelArray(builder, CommunicationPacket1Labels),
                false),
            builder.NewVariable(
                "communication_packet_2_values",
                CreateCommunicationPacket2Matrix(builder, communicationPacket2Records),
                false),
            builder.NewVariable(
                "communication_packet_2_labels",
                CreateLabelArray(builder, CommunicationPacket2Labels),
                false)
        ]);

        using FileStream fileStream = new(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        MatFileWriter writer = new(fileStream);
        writer.Write(matFile);
    }

    private static IArray CreateCommunicationPacket1Matrix(
        DataBuilder builder,
        IReadOnlyList<CommunicationLogRecord> records)
    {
        IArrayOf<double> matrix = builder.NewArray<double>([records.Count, CommunicationPacket1Labels.Length]);

        for (int row = 0; row < records.Count; row++)
        {
            CommunicationLogRecord record = records[row];

            matrix[[row, 0]] = ToDouble(record.Data1);
            matrix[[row, 1]] = ToDouble(record.Data2);
            matrix[[row, 2]] = ToDouble(record.Data3);
            matrix[[row, 3]] = ToDouble(record.Data4);
            matrix[[row, 4]] = ToDouble(record.Data5);
            matrix[[row, 5]] = ToDouble(record.Data6Float);
            matrix[[row, 6]] = ToDouble(record.Data7Float);
        }

        return matrix;
    }

    private static IArray CreateCommunicationPacket2Matrix(
        DataBuilder builder,
        IReadOnlyList<CommunicationLogRecord> records)
    {
        IArrayOf<double> matrix = builder.NewArray<double>([records.Count, CommunicationPacket2Labels.Length]);

        for (int row = 0; row < records.Count; row++)
        {
            CommunicationLogRecord record = records[row];

            matrix[[row, 0]] = ToDouble(record.Data1);
            matrix[[row, 1]] = ToDouble(record.Data2);
            matrix[[row, 2]] = ToDouble(record.Data3);
            matrix[[row, 3]] = ToDouble(record.Data4);
            matrix[[row, 4]] = ToDouble(record.SystemTime);
            matrix[[row, 5]] = ToDouble(record.Data6Double);
        }

        return matrix;
    }

    private static IArray CreateLabelArray(DataBuilder builder, IReadOnlyList<string> labels)
    {
        ICellArray labelArray = builder.NewCellArray([1, labels.Count]);

        for (int index = 0; index < labels.Count; index++)
        {
            labelArray[[0, index]] = builder.NewCharArray(labels[index]);
        }

        return labelArray;
    }

    private static double ToDouble<T>(T? value)
        where T : struct, IConvertible
    {
        return value.HasValue
            ? value.Value.ToDouble(CultureInfo.InvariantCulture)
            : double.NaN;
    }

    private static string GetMatlabDirectoryPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Release", "MATLAB Dönüsümleri");
    }
}
