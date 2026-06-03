using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Baykar.Shared.Communication;
using Baykar.Shared.Enums;
using Baykar.Shared.Models;
using Baykar.Shared.Packets;
using Baykar.SimulationInterface.Commands;

namespace Baykar.SimulationInterface.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const int LocalPort = 5000;
    private const int RemotePort = 5001;
    private const string RemoteIp = "127.0.0.1";
    private const int TransmissionIntervalMilliseconds = 200;

    private readonly UdpCommunicationService _udpCommunicationService = new();
    private readonly PacketCaptureStateMachine _packetCaptureStateMachine = new();

    private CancellationTokenSource? _listenerCancellationTokenSource;
    private CancellationTokenSource? _transmissionCancellationTokenSource;
    private string _simulationStatus = "Stopped";
    private string _listenerStatus = "Stopped";
    private int _communicationPacket1SentCount;
    private int _communicationPacket2SentCount;
    private uint _systemTime = 1;
    private string _lastReceivedCommand = "None";
    private string _lastReceivedSetting = "None";
    private string _lastSettingValue = "0";
    private string _lastSentFeedback = "None";
    private string _lastReceivedPacketBytes = string.Empty;
    private int _validPacketCount;
    private int _invalidPacketCount;
    private string _parserErrorMessage = "No parser errors";
    private string _lastBuiltCommunicationPacket1Bytes = string.Empty;
    private string _lastBuiltCommunicationPacket2Bytes = string.Empty;
    private string _packetValidationResult = "No packet validation yet";
    private byte[]? _lastBuiltCommunicationPacket1;
    private byte[]? _lastBuiltCommunicationPacket2;

    public MainViewModel()
    {
        StartSimulationCommand = new RelayCommand(StartSimulation);
        StopSimulationCommand = new RelayCommand(StopSimulation);
        ResetCountersCommand = new RelayCommand(ResetCounters);
        ValidateLastBuiltPacketsCommand = new RelayCommand(ValidateLastBuiltPackets);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SimulationStatus
    {
        get => _simulationStatus;
        private set => SetProperty(ref _simulationStatus, value);
    }

    public string ListenerStatus
    {
        get => _listenerStatus;
        private set => SetProperty(ref _listenerStatus, value);
    }

    public string SelectedProtocol { get; } = "UDP";

    public string LocalPortText { get; } = "5000";

    public string RemotePortText { get; } = "5001";

    public int CommunicationPacket1SentCount
    {
        get => _communicationPacket1SentCount;
        private set => SetProperty(ref _communicationPacket1SentCount, value);
    }

    public int CommunicationPacket2SentCount
    {
        get => _communicationPacket2SentCount;
        private set => SetProperty(ref _communicationPacket2SentCount, value);
    }

    public uint SystemTime
    {
        get => _systemTime;
        private set => SetProperty(ref _systemTime, value);
    }

    public string LastReceivedCommand
    {
        get => _lastReceivedCommand;
        private set => SetProperty(ref _lastReceivedCommand, value);
    }

    public string LastReceivedSetting
    {
        get => _lastReceivedSetting;
        private set => SetProperty(ref _lastReceivedSetting, value);
    }

    public string LastSettingValue
    {
        get => _lastSettingValue;
        private set => SetProperty(ref _lastSettingValue, value);
    }

    public string LastSentFeedback
    {
        get => _lastSentFeedback;
        private set => SetProperty(ref _lastSentFeedback, value);
    }

    public string LastReceivedPacketBytes
    {
        get => _lastReceivedPacketBytes;
        private set => SetProperty(ref _lastReceivedPacketBytes, value);
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

    public string ParserErrorMessage
    {
        get => _parserErrorMessage;
        private set => SetProperty(ref _parserErrorMessage, value);
    }

    public string LastBuiltCommunicationPacket1Bytes
    {
        get => _lastBuiltCommunicationPacket1Bytes;
        private set => SetProperty(ref _lastBuiltCommunicationPacket1Bytes, value);
    }

    public string LastBuiltCommunicationPacket2Bytes
    {
        get => _lastBuiltCommunicationPacket2Bytes;
        private set => SetProperty(ref _lastBuiltCommunicationPacket2Bytes, value);
    }

    public string PacketValidationResult
    {
        get => _packetValidationResult;
        private set => SetProperty(ref _packetValidationResult, value);
    }

    public ICommand StartSimulationCommand { get; }

    public ICommand StopSimulationCommand { get; }

    public ICommand ResetCountersCommand { get; }

    public ICommand ValidateLastBuiltPacketsCommand { get; }

    private void StartSimulation()
    {
        SimulationStatus = "Running";
        StartListenerIfNeeded();
        StartTransmissionLoopIfNeeded();
    }

    private void StopSimulation()
    {
        _transmissionCancellationTokenSource?.Cancel();
        _listenerCancellationTokenSource?.Cancel();
        _udpCommunicationService.StopListening();
        SimulationStatus = "Stopped";
        ListenerStatus = "Stopped";
    }

    private void StartListenerIfNeeded()
    {
        if (_listenerCancellationTokenSource is not null)
        {
            return;
        }

        CancellationTokenSource cancellationTokenSource = new();
        _listenerCancellationTokenSource = cancellationTokenSource;
        _udpCommunicationService.BytesReceived += OnBytesReceived;
        ListenerStatus = $"Listening on port {LocalPort}";

        _ = ListenAsync(cancellationTokenSource);
    }

    private async Task ListenAsync(CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await _udpCommunicationService.StartListeningAsync(LocalPort, cancellationTokenSource.Token);
        }
        catch (Exception exception) when (!cancellationTokenSource.IsCancellationRequested)
        {
            RunOnUi(() =>
            {
                ListenerStatus = "Stopped";
                ParserErrorMessage = $"Listener error: {exception.Message}";
            });
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

    private void StartTransmissionLoopIfNeeded()
    {
        if (_transmissionCancellationTokenSource is not null)
        {
            return;
        }

        CancellationTokenSource cancellationTokenSource = new();
        _transmissionCancellationTokenSource = cancellationTokenSource;

        _ = RunTransmissionLoopAsync(cancellationTokenSource);
    }

    private async Task RunTransmissionLoopAsync(CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await SendCommunicationPacketsAsync();
                await Task.Delay(TransmissionIntervalMilliseconds, cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (SocketException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            RunOnUi(() => ParserErrorMessage = $"Transmission error: {exception.Message}");
        }
        finally
        {
            cancellationTokenSource.Dispose();

            if (ReferenceEquals(_transmissionCancellationTokenSource, cancellationTokenSource))
            {
                _transmissionCancellationTokenSource = null;
            }
        }
    }

    private async Task SendCommunicationPacketsAsync()
    {
        CommunicationPacket1Payload communicationPacket1Payload = new(
            data1: 17,
            data2: 26,
            data3: 5,
            data4: -435,
            data5: 0,
            data6: 12.7f,
            data7: 42.68f);

        CommunicationPacket2Payload communicationPacket2Payload = new(
            data1: 254,
            data2: 176,
            data3: -278,
            data4: -65425,
            systemTime: SystemTime,
            data6: 6541.3154);

        byte[] communicationPacket1 = PacketBuilder.Build(
            PacketType.CommunicationPacket1,
            communicationPacket1Payload.ToByteArray());

        byte[] communicationPacket2 = PacketBuilder.Build(
            PacketType.CommunicationPacket2,
            communicationPacket2Payload.ToByteArray());

        await _udpCommunicationService.SendAsync(communicationPacket1, RemoteIp, RemotePort);
        await _udpCommunicationService.SendAsync(communicationPacket2, RemoteIp, RemotePort);

        RunOnUi(() =>
        {
            _lastBuiltCommunicationPacket1 = communicationPacket1.ToArray();
            _lastBuiltCommunicationPacket2 = communicationPacket2.ToArray();
            LastBuiltCommunicationPacket1Bytes = ToHexString(communicationPacket1);
            LastBuiltCommunicationPacket2Bytes = ToHexString(communicationPacket2);
            CommunicationPacket1SentCount++;
            CommunicationPacket2SentCount++;
            SystemTime++;
        });
    }

    private void ResetCounters()
    {
        CommunicationPacket1SentCount = 0;
        CommunicationPacket2SentCount = 0;
        SystemTime = 1;
        _lastBuiltCommunicationPacket1 = null;
        _lastBuiltCommunicationPacket2 = null;
        LastBuiltCommunicationPacket1Bytes = string.Empty;
        LastBuiltCommunicationPacket2Bytes = string.Empty;
        PacketValidationResult = "No packet validation yet";
    }

    private void ValidateLastBuiltPackets()
    {
        if (_lastBuiltCommunicationPacket1 is null || _lastBuiltCommunicationPacket2 is null)
        {
            PacketValidationResult = "No built communication packets are available for validation.";
            return;
        }

        PacketParseResult packet1Result = PacketValidator.Validate(_lastBuiltCommunicationPacket1);
        PacketParseResult packet2Result = PacketValidator.Validate(_lastBuiltCommunicationPacket2);

        PacketValidationResult = packet1Result.IsSuccess && packet2Result.IsSuccess
            ? "Both built communication packets are valid."
            : $"Packet 1: {GetResultText(packet1Result)} Packet 2: {GetResultText(packet2Result)}";
    }

    private void OnBytesReceived(object? sender, byte[] bytes)
    {
        RunOnUi(() => ProcessReceivedBytes(bytes));
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

            ParserErrorMessage = $"Packet parse error: {result.ErrorMessage}";
        }
    }

    private void HandleReceivedPacket(ParsedPacket packet)
    {
        LastReceivedPacketBytes = ToHexString(packet.RawBytes);

        try
        {
            switch (packet.PacketType)
            {
                case PacketType.CommandPacket:
                    HandleCommandPacket(CommandPacketPayload.FromByteArray(packet.Payload));
                    break;

                case PacketType.SettingPacket:
                    HandleSettingPacket(SettingPacketPayload.FromByteArray(packet.Payload));
                    break;
            }
        }
        catch (ArgumentException exception)
        {
            ParserErrorMessage = $"Payload parse error: {exception.Message}";
        }
    }

    private void HandleCommandPacket(CommandPacketPayload payload)
    {
        LastReceivedCommand = payload.Command.ToString();
        _ = SendFeedbackPacketAsync((byte)payload.Command, 1);
    }

    private void HandleSettingPacket(SettingPacketPayload payload)
    {
        LastReceivedSetting = payload.Setting.ToString();
        LastSettingValue = payload.Value.ToString(CultureInfo.InvariantCulture);
        _ = SendFeedbackPacketAsync((byte)payload.Setting, payload.Value);
    }

    private async Task SendFeedbackPacketAsync(byte responseType, float value)
    {
        FeedbackPacketPayload payload = new(responseType, value);
        byte[] packet = PacketBuilder.Build(PacketType.FeedbackPacket, payload.ToByteArray());

        try
        {
            await _udpCommunicationService.SendAsync(packet, RemoteIp, RemotePort);
            RunOnUi(() => LastSentFeedback = $"ResponseType={responseType}, Value={value.ToString(CultureInfo.InvariantCulture)}");
        }
        catch (Exception exception)
        {
            RunOnUi(() => ParserErrorMessage = $"Feedback send failed: {exception.Message}");
        }
    }

    private static string GetResultText(PacketParseResult result)
    {
        return result.IsSuccess
            ? "Valid."
            : $"Invalid - {result.ErrorMessage}";
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
