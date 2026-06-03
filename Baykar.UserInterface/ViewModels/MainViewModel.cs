using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Baykar.Shared.Communication;
using Baykar.Shared.Enums;
using Baykar.Shared.Logging;
using Baykar.Shared.Matlab;
using Baykar.Shared.Models;
using Baykar.Shared.Packets;
using Baykar.UserInterface.Commands;

namespace Baykar.UserInterface.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const int LocalPort = 5001;
    private const int RemotePort = 5000;
    private const string RemoteIp = "127.0.0.1";

    private readonly UdpCommunicationService _udpCommunicationService = new();
    private readonly PacketCaptureStateMachine _packetCaptureStateMachine = new();
    private readonly CommunicationLogService _communicationLogService = new();
    private readonly MatlabConversionService _matlabConversionService = new();

    private CancellationTokenSource? _listenerCancellationTokenSource;
    private CancellationTokenSource? _playbackCancellationTokenSource;
    private string _listenerStatus = "Stopped";
    private string _lastAction = "No action yet";
    private string _setting1Value = "12.5";
    private string _setting2Value = "44.7";
    private string _setting3Value = "30";
    private string _setting4Value = "75";
    private string _lastBuiltPacketType = "None";
    private int _lastBuiltPayloadLength;
    private string _lastBuiltCrc = string.Empty;
    private string _lastBuiltPacketBytes = string.Empty;
    private string _lastReceivedPacketType = "None";
    private string _lastReceivedPacketBytes = string.Empty;
    private string _lastCommunicationPacket1Bytes = string.Empty;
    private string _lastCommunicationPacket2Bytes = string.Empty;
    private string _lastCommunicationPacket1ReceivedAt = string.Empty;
    private string _lastCommunicationPacket2ReceivedAt = string.Empty;
    private string _lastReceivedFeedback = "No feedback received yet";
    private int _validPacketCount;
    private int _invalidPacketCount;
    private int _communicationPacket1ReceivedCount;
    private int _communicationPacket2ReceivedCount;
    private IReadOnlyList<DisplayRow> _communicationPacket1Rows = CreateInitialCommunicationPacket1Rows();
    private IReadOnlyList<DisplayRow> _communicationPacket2Rows = CreateInitialCommunicationPacket2Rows();
    private bool _isLogging;
    private string _loggingStatus = "Logging stopped";
    private string _currentLogFilePath = string.Empty;
    private string _playbackStatus = "Playback stopped";
    private string _selectedPlaybackFilePath = string.Empty;
    private string _matlabConversionStatus = "Not converted";
    private string _matlabOutputFilePath = string.Empty;
    private byte[]? _lastBuiltPacket;

    public MainViewModel()
    {
        StartListenerCommand = new RelayCommand(() => _ = StartListenerAsync());
        StopListenerCommand = new RelayCommand(StopListener);

        SendCommand1Command = new RelayCommand(async () => await BuildAndSendCommandPacketAsync(CommandType.Command1, "Command 1"));
        SendCommand2Command = new RelayCommand(async () => await BuildAndSendCommandPacketAsync(CommandType.Command2, "Command 2"));
        SendCommand3Command = new RelayCommand(async () => await BuildAndSendCommandPacketAsync(CommandType.Command3, "Command 3"));

        SendSetting1Command = new RelayCommand(async () => await BuildAndSendSettingPacketAsync(SettingType.Setting1, Setting1Value, "Setting 1"));
        SendSetting2Command = new RelayCommand(async () => await BuildAndSendSettingPacketAsync(SettingType.Setting2, Setting2Value, "Setting 2"));
        SendSetting3Command = new RelayCommand(async () => await BuildAndSendSettingPacketAsync(SettingType.Setting3, Setting3Value, "Setting 3"));
        SendSetting4Command = new RelayCommand(async () => await BuildAndSendSettingPacketAsync(SettingType.Setting4, Setting4Value, "Setting 4"));

        StartLoggingCommand = new RelayCommand(StartLogging);
        StopLoggingCommand = new RelayCommand(StopLogging);
        PlayLogCommand = new RelayCommand(async () => await PlayLogAsync());
        StopPlaybackCommand = new RelayCommand(StopPlayback);
        ConvertToMatlabCommand = new RelayCommand(async () => await ConvertToMatlabAsync());
        ValidateLastBuiltPacketCommand = new RelayCommand(ValidateLastBuiltPacket);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DisplayRow> ConnectionRows { get; } = new List<DisplayRow>
    {
        new("Connection Status:", "Not Connected"),
        new("Selected Protocol:", "UDP"),
        new("Local Port:", "5001"),
        new("Remote Port:", "5000")
    };

    public IReadOnlyList<DisplayRow> CommunicationPacket1Rows
    {
        get => _communicationPacket1Rows;
        private set => SetProperty(ref _communicationPacket1Rows, value);
    }

    public IReadOnlyList<DisplayRow> CommunicationPacket2Rows
    {
        get => _communicationPacket2Rows;
        private set => SetProperty(ref _communicationPacket2Rows, value);
    }

    public string ListenerStatus
    {
        get => _listenerStatus;
        private set => SetProperty(ref _listenerStatus, value);
    }

    public int ValidPacketCount
    {
        get => _validPacketCount;
        private set => SetProperty(ref _validPacketCount, value);
    }

    public int InvalidPacketCount
    {
        get => _invalidPacketCount;
        private set => SetProperty(ref _invalidPacketCount, value);
    }

    public string LastReceivedPacketType
    {
        get => _lastReceivedPacketType;
        private set => SetProperty(ref _lastReceivedPacketType, value);
    }

    public string LastReceivedPacketBytes
    {
        get => _lastReceivedPacketBytes;
        private set => SetProperty(ref _lastReceivedPacketBytes, value);
    }

    public string LastCommunicationPacket1Bytes
    {
        get => _lastCommunicationPacket1Bytes;
        private set => SetProperty(ref _lastCommunicationPacket1Bytes, value);
    }

    public string LastCommunicationPacket2Bytes
    {
        get => _lastCommunicationPacket2Bytes;
        private set => SetProperty(ref _lastCommunicationPacket2Bytes, value);
    }

    public string LastCommunicationPacket1ReceivedAt
    {
        get => _lastCommunicationPacket1ReceivedAt;
        private set => SetProperty(ref _lastCommunicationPacket1ReceivedAt, value);
    }

    public string LastCommunicationPacket2ReceivedAt
    {
        get => _lastCommunicationPacket2ReceivedAt;
        private set => SetProperty(ref _lastCommunicationPacket2ReceivedAt, value);
    }

    public int CommunicationPacket1ReceivedCount
    {
        get => _communicationPacket1ReceivedCount;
        private set => SetProperty(ref _communicationPacket1ReceivedCount, value);
    }

    public int CommunicationPacket2ReceivedCount
    {
        get => _communicationPacket2ReceivedCount;
        private set => SetProperty(ref _communicationPacket2ReceivedCount, value);
    }

    public string LastReceivedFeedback
    {
        get => _lastReceivedFeedback;
        private set => SetProperty(ref _lastReceivedFeedback, value);
    }

    public string Setting1Value
    {
        get => _setting1Value;
        set => SetProperty(ref _setting1Value, value);
    }

    public string Setting2Value
    {
        get => _setting2Value;
        set => SetProperty(ref _setting2Value, value);
    }

    public string Setting3Value
    {
        get => _setting3Value;
        set => SetProperty(ref _setting3Value, value);
    }

    public string Setting4Value
    {
        get => _setting4Value;
        set => SetProperty(ref _setting4Value, value);
    }

    public string LastAction
    {
        get => _lastAction;
        private set => SetProperty(ref _lastAction, value);
    }

    public string LastBuiltPacketType
    {
        get => _lastBuiltPacketType;
        private set => SetProperty(ref _lastBuiltPacketType, value);
    }

    public int LastBuiltPayloadLength
    {
        get => _lastBuiltPayloadLength;
        private set => SetProperty(ref _lastBuiltPayloadLength, value);
    }

    public string LastBuiltCrc
    {
        get => _lastBuiltCrc;
        private set => SetProperty(ref _lastBuiltCrc, value);
    }

    public string LastBuiltPacketBytes
    {
        get => _lastBuiltPacketBytes;
        private set => SetProperty(ref _lastBuiltPacketBytes, value);
    }

    public bool IsLogging
    {
        get => _isLogging;
        private set => SetProperty(ref _isLogging, value);
    }

    public string LoggingStatus
    {
        get => _loggingStatus;
        private set => SetProperty(ref _loggingStatus, value);
    }

    public string CurrentLogFilePath
    {
        get => _currentLogFilePath;
        private set => SetProperty(ref _currentLogFilePath, value);
    }

    public string PlaybackStatus
    {
        get => _playbackStatus;
        private set => SetProperty(ref _playbackStatus, value);
    }

    public string SelectedPlaybackFilePath
    {
        get => _selectedPlaybackFilePath;
        set => SetProperty(ref _selectedPlaybackFilePath, value);
    }

    public string MatlabConversionStatus
    {
        get => _matlabConversionStatus;
        private set => SetProperty(ref _matlabConversionStatus, value);
    }

    public string MatlabOutputFilePath
    {
        get => _matlabOutputFilePath;
        private set => SetProperty(ref _matlabOutputFilePath, value);
    }

    public ICommand StartListenerCommand { get; }

    public ICommand StopListenerCommand { get; }

    public ICommand SendCommand1Command { get; }

    public ICommand SendCommand2Command { get; }

    public ICommand SendCommand3Command { get; }

    public ICommand SendSetting1Command { get; }

    public ICommand SendSetting2Command { get; }

    public ICommand SendSetting3Command { get; }

    public ICommand SendSetting4Command { get; }

    public ICommand StartLoggingCommand { get; }

    public ICommand StopLoggingCommand { get; }

    public ICommand PlayLogCommand { get; }

    public ICommand StopPlaybackCommand { get; }

    public ICommand ConvertToMatlabCommand { get; }

    public ICommand ValidateLastBuiltPacketCommand { get; }

    private async Task StartListenerAsync()
    {
        if (_listenerCancellationTokenSource is not null)
        {
            LastAction = "Listener is already running.";
            return;
        }

        CancellationTokenSource cancellationTokenSource = new();
        _listenerCancellationTokenSource = cancellationTokenSource;
        _udpCommunicationService.BytesReceived += OnBytesReceived;
        ListenerStatus = $"Listening on port {LocalPort}";

        try
        {
            await _udpCommunicationService.StartListeningAsync(LocalPort, cancellationTokenSource.Token);
        }
        catch (Exception exception) when (!cancellationTokenSource.IsCancellationRequested)
        {
            ListenerStatus = "Stopped";
            LastAction = $"Listener error: {exception.Message}";
        }
        finally
        {
            _udpCommunicationService.BytesReceived -= OnBytesReceived;
            cancellationTokenSource.Dispose();

            if (ReferenceEquals(_listenerCancellationTokenSource, cancellationTokenSource))
            {
                _listenerCancellationTokenSource = null;
            }
        }
    }

    private void StopListener()
    {
        if (_listenerCancellationTokenSource is null)
        {
            ListenerStatus = "Stopped";
            return;
        }

        _listenerCancellationTokenSource.Cancel();
        _udpCommunicationService.StopListening();
        ListenerStatus = "Stopped";
    }

    private void StartLogging()
    {
        CurrentLogFilePath = _communicationLogService.CreateNewLogFilePath();
        IsLogging = true;
        LoggingStatus = "Logging started";
    }

    private void StopLogging()
    {
        IsLogging = false;
        LoggingStatus = "Logging stopped";
    }

    private async Task PlayLogAsync()
    {
        if (_playbackCancellationTokenSource is not null)
        {
            PlaybackStatus = "Playback is already running";
            return;
        }

        string filePath = GetPlaybackFilePath();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            PlaybackStatus = "No log file found";
            return;
        }

        SelectedPlaybackFilePath = filePath;
        CancellationTokenSource cancellationTokenSource = new();
        _playbackCancellationTokenSource = cancellationTokenSource;

        try
        {
            IReadOnlyList<CommunicationLogRecord> records = await _communicationLogService.ReadAsync(filePath, cancellationTokenSource.Token);

            if (records.Count == 0)
            {
                PlaybackStatus = "No records found";
                return;
            }

            for (int index = 0; index < records.Count; index++)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                CommunicationLogRecord record = records[index];

                RunOnUi(() =>
                {
                    ApplyPlaybackRecord(record);
                    PlaybackStatus = $"Playing {index + 1}/{records.Count}";
                });

                await Task.Delay(200, cancellationTokenSource.Token);
            }

            PlaybackStatus = "Playback completed";
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            PlaybackStatus = "Playback stopped";
        }
        catch (Exception exception)
        {
            PlaybackStatus = $"Playback error: {exception.Message}";
        }
        finally
        {
            cancellationTokenSource.Dispose();

            if (ReferenceEquals(_playbackCancellationTokenSource, cancellationTokenSource))
            {
                _playbackCancellationTokenSource = null;
            }
        }
    }

    private void StopPlayback()
    {
        if (_playbackCancellationTokenSource is null)
        {
            PlaybackStatus = "Playback stopped";
            return;
        }

        _playbackCancellationTokenSource.Cancel();
        PlaybackStatus = "Playback stopped";
    }

    private async Task ConvertToMatlabAsync()
    {
        string filePath = GetPlaybackFilePath();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            MatlabConversionStatus = "No log file found to convert";
            MatlabOutputFilePath = string.Empty;
            return;
        }

        SelectedPlaybackFilePath = filePath;
        MatlabConversionStatus = "Conversion started";
        MatlabOutputFilePath = string.Empty;

        try
        {
            MatlabConversionResult result = await _matlabConversionService.ConvertLogToMatAsync(filePath, CancellationToken.None);

            if (result.IsSuccess)
            {
                MatlabConversionStatus = "Conversion completed";
                MatlabOutputFilePath = result.OutputFilePath;
                LastAction = "MATLAB conversion completed";
                return;
            }

            MatlabConversionStatus = $"Conversion failed: {result.ErrorMessage}";
            LastAction = "MATLAB conversion failed";
        }
        catch (Exception exception)
        {
            MatlabConversionStatus = $"Conversion error: {exception.Message}";
            LastAction = "MATLAB conversion failed";
        }
    }

    private async Task BuildAndSendCommandPacketAsync(CommandType command, string commandName)
    {
        CommandPacketPayload payload = new(command);
        byte[] payloadBytes = payload.ToByteArray();
        byte[] packet = PacketBuilder.Build(PacketType.CommandPacket, payloadBytes);

        DisplayBuiltPacket(PacketType.CommandPacket, payloadBytes.Length, packet);

        try
        {
            await _udpCommunicationService.SendAsync(packet, RemoteIp, RemotePort);
            LastAction = $"{commandName} packet sent";
        }
        catch (Exception exception)
        {
            LastAction = $"{commandName} packet send failed: {exception.Message}";
        }
    }

    private async Task BuildAndSendSettingPacketAsync(SettingType setting, string valueText, string settingName)
    {
        if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            LastAction = $"{settingName} value must be a valid number.";
            return;
        }

        SettingPacketPayload payload = new(setting, value);
        byte[] payloadBytes = payload.ToByteArray();
        byte[] packet = PacketBuilder.Build(PacketType.SettingPacket, payloadBytes);

        DisplayBuiltPacket(PacketType.SettingPacket, payloadBytes.Length, packet);

        try
        {
            await _udpCommunicationService.SendAsync(packet, RemoteIp, RemotePort);
            LastAction = $"{settingName} packet sent";
        }
        catch (Exception exception)
        {
            LastAction = $"{settingName} packet send failed: {exception.Message}";
        }
    }

    private void DisplayBuiltPacket(PacketType packetType, int payloadLength, byte[] packet)
    {
        _lastBuiltPacket = packet.ToArray();
        LastBuiltPacketType = packetType.ToString();
        LastBuiltPayloadLength = payloadLength;
        LastBuiltCrc = packet[^1].ToString("X2", CultureInfo.InvariantCulture);
        LastBuiltPacketBytes = ToHexString(packet);
    }

    private void ValidateLastBuiltPacket()
    {
        if (_lastBuiltPacket is null)
        {
            LastAction = "No built packet is available for validation.";
            return;
        }

        PacketParseResult result = PacketValidator.Validate(_lastBuiltPacket);

        LastAction = result.IsSuccess && result.Packet is not null
            ? $"Last built {result.Packet.PacketType} packet is valid."
            : $"Last built packet is invalid: {result.ErrorMessage}";
    }

    private void OnBytesReceived(object? sender, byte[] bytes)
    {
        Application.Current.Dispatcher.Invoke(() => ProcessReceivedBytes(bytes));
    }

    private void ProcessReceivedBytes(byte[] bytes)
    {
        foreach (byte incomingByte in bytes)
        {
            PacketParseResult? result = _packetCaptureStateMachine.ProcessByte(incomingByte);

            if (result is null)
            {
                continue;
            }

            ValidPacketCount = _packetCaptureStateMachine.ValidPacketCount;
            InvalidPacketCount = _packetCaptureStateMachine.InvalidPacketCount;

            if (result.IsSuccess && result.Packet is not null)
            {
                HandleReceivedPacket(result.Packet);
                continue;
            }

            LastAction = $"Packet parse error: {result.ErrorMessage}";
        }
    }

    private void HandleReceivedPacket(ParsedPacket packet)
    {
        LastReceivedPacketType = packet.PacketType.ToString();
        LastReceivedPacketBytes = ToHexString(packet.RawBytes);

        try
        {
            switch (packet.PacketType)
            {
                case PacketType.CommunicationPacket1:
                    CommunicationPacket1Payload communicationPacket1Payload = CommunicationPacket1Payload.FromByteArray(packet.Payload);
                    UpdateCommunicationPacket1Rows(communicationPacket1Payload);
                    UpdateCommunicationPacket1ReceiveDetails(packet);
                    AppendLogRecordIfNeeded(CreateLogRecord(communicationPacket1Payload));
                    break;

                case PacketType.CommunicationPacket2:
                    CommunicationPacket2Payload communicationPacket2Payload = CommunicationPacket2Payload.FromByteArray(packet.Payload);
                    UpdateCommunicationPacket2Rows(communicationPacket2Payload);
                    UpdateCommunicationPacket2ReceiveDetails(packet);
                    AppendLogRecordIfNeeded(CreateLogRecord(communicationPacket2Payload));
                    break;

                case PacketType.FeedbackPacket:
                    UpdateLastReceivedFeedback(FeedbackPacketPayload.FromByteArray(packet.Payload));
                    break;
            }
        }
        catch (ArgumentException exception)
        {
            LastAction = $"Payload parse error: {exception.Message}";
        }
    }

    private void UpdateCommunicationPacket1Rows(CommunicationPacket1Payload payload)
    {
        CommunicationPacket1Rows = new List<DisplayRow>
        {
            new("Data1:", payload.Data1.ToString(CultureInfo.InvariantCulture)),
            new("Data2:", payload.Data2.ToString(CultureInfo.InvariantCulture)),
            new("Data3:", payload.Data3.ToString(CultureInfo.InvariantCulture)),
            new("Data4:", payload.Data4.ToString(CultureInfo.InvariantCulture)),
            new("Data5:", payload.Data5.ToString(CultureInfo.InvariantCulture)),
            new("Data6:", payload.Data6.ToString(CultureInfo.InvariantCulture)),
            new("Data7:", payload.Data7.ToString(CultureInfo.InvariantCulture))
        };
    }

    private void UpdateCommunicationPacket2Rows(CommunicationPacket2Payload payload)
    {
        CommunicationPacket2Rows = new List<DisplayRow>
        {
            new("Data1:", payload.Data1.ToString(CultureInfo.InvariantCulture)),
            new("Data2:", payload.Data2.ToString(CultureInfo.InvariantCulture)),
            new("Data3:", payload.Data3.ToString(CultureInfo.InvariantCulture)),
            new("Data4:", payload.Data4.ToString(CultureInfo.InvariantCulture)),
            new("SystemTime:", payload.SystemTime.ToString(CultureInfo.InvariantCulture)),
            new("Data6:", payload.Data6.ToString(CultureInfo.InvariantCulture))
        };
    }

    private void UpdateLastReceivedFeedback(FeedbackPacketPayload payload)
    {
        LastReceivedFeedback = $"ResponseType={payload.ResponseType}, Value={payload.Value.ToString(CultureInfo.InvariantCulture)}";
    }

    private void UpdateCommunicationPacket1ReceiveDetails(ParsedPacket packet)
    {
        LastCommunicationPacket1Bytes = ToHexString(packet.RawBytes);
        LastCommunicationPacket1ReceivedAt = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        CommunicationPacket1ReceivedCount++;
    }

    private void UpdateCommunicationPacket2ReceiveDetails(ParsedPacket packet)
    {
        LastCommunicationPacket2Bytes = ToHexString(packet.RawBytes);
        LastCommunicationPacket2ReceivedAt = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        CommunicationPacket2ReceivedCount++;
    }

    private void AppendLogRecordIfNeeded(CommunicationLogRecord record)
    {
        if (!IsLogging)
        {
            return;
        }

        _ = AppendLogRecordAsync(record);
    }

    private async Task AppendLogRecordAsync(CommunicationLogRecord record)
    {
        try
        {
            await _communicationLogService.AppendAsync(record, CancellationToken.None);
        }
        catch (Exception exception)
        {
            RunOnUi(() => LoggingStatus = $"Logging error: {exception.Message}");
        }
    }

    private static CommunicationLogRecord CreateLogRecord(CommunicationPacket1Payload payload)
    {
        return new CommunicationLogRecord
        {
            TimestampUtc = DateTime.UtcNow,
            PacketType = PacketType.CommunicationPacket1,
            Data1 = payload.Data1,
            Data2 = payload.Data2,
            Data3 = payload.Data3,
            Data4 = payload.Data4,
            Data5 = payload.Data5,
            Data6Float = payload.Data6,
            Data7Float = payload.Data7
        };
    }

    private static CommunicationLogRecord CreateLogRecord(CommunicationPacket2Payload payload)
    {
        return new CommunicationLogRecord
        {
            TimestampUtc = DateTime.UtcNow,
            PacketType = PacketType.CommunicationPacket2,
            Data1 = payload.Data1,
            Data2 = payload.Data2,
            Data3 = payload.Data3,
            Data4 = payload.Data4,
            SystemTime = payload.SystemTime,
            Data6Double = payload.Data6
        };
    }

    private void ApplyPlaybackRecord(CommunicationLogRecord record)
    {
        switch (record.PacketType)
        {
            case PacketType.CommunicationPacket1:
                ApplyCommunicationPacket1PlaybackRecord(record);
                break;

            case PacketType.CommunicationPacket2:
                ApplyCommunicationPacket2PlaybackRecord(record);
                break;
        }
    }

    private void ApplyCommunicationPacket1PlaybackRecord(CommunicationLogRecord record)
    {
        CommunicationPacket1Rows = new List<DisplayRow>
        {
            new("Data1:", FormatNullable(record.Data1)),
            new("Data2:", FormatNullable(record.Data2)),
            new("Data3:", FormatNullable(record.Data3)),
            new("Data4:", FormatNullable(record.Data4)),
            new("Data5:", FormatNullable(record.Data5)),
            new("Data6:", FormatNullable(record.Data6Float)),
            new("Data7:", FormatNullable(record.Data7Float))
        };
    }

    private void ApplyCommunicationPacket2PlaybackRecord(CommunicationLogRecord record)
    {
        CommunicationPacket2Rows = new List<DisplayRow>
        {
            new("Data1:", FormatNullable(record.Data1)),
            new("Data2:", FormatNullable(record.Data2)),
            new("Data3:", FormatNullable(record.Data3)),
            new("Data4:", FormatNullable(record.Data4)),
            new("SystemTime:", FormatNullable(record.SystemTime)),
            new("Data6:", FormatNullable(record.Data6Double))
        };
    }

    private static string GetLogDirectoryPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Release", "Log Kayıtları");
    }

    private string GetPlaybackFilePath()
    {
        if (!string.IsNullOrWhiteSpace(SelectedPlaybackFilePath))
        {
            return SelectedPlaybackFilePath;
        }

        string directoryPath = GetLogDirectoryPath();

        if (!Directory.Exists(directoryPath))
        {
            return string.Empty;
        }

        return Directory
            .GetFiles(directoryPath, "*.csv")
            .Select(filePath => new FileInfo(filePath))
            .OrderByDescending(fileInfo => fileInfo.LastWriteTimeUtc)
            .FirstOrDefault()
            ?.FullName ?? string.Empty;
    }

    private static string FormatNullable<T>(T? value)
        where T : struct, IFormattable
    {
        return value.HasValue
            ? value.Value.ToString(null, CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static IReadOnlyList<DisplayRow> CreateInitialCommunicationPacket1Rows()
    {
        return new List<DisplayRow>
        {
            new("Data1:", "17"),
            new("Data2:", "26"),
            new("Data3:", "5"),
            new("Data4:", "-435"),
            new("Data5:", "0"),
            new("Data6:", "12.7"),
            new("Data7:", "42.68")
        };
    }

    private static IReadOnlyList<DisplayRow> CreateInitialCommunicationPacket2Rows()
    {
        return new List<DisplayRow>
        {
            new("Data1:", "254"),
            new("Data2:", "176"),
            new("Data3:", "-278"),
            new("Data4:", "-65425"),
            new("SystemTime:", "1"),
            new("Data6:", "6541.3154")
        };
    }

    private static string ToHexString(byte[] bytes)
    {
        return string.Join(" ", bytes.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
    }

    private static void RunOnUi(Action action)
    {
        Application.Current.Dispatcher.Invoke(action);
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record DisplayRow(string Label, string Value);
