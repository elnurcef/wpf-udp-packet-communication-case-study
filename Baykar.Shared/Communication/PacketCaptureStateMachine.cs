using Baykar.Shared.Models;

namespace Baykar.Shared.Communication;

public sealed class PacketCaptureStateMachine
{
    private readonly List<byte> _buffer = [];
    private CaptureState _state = CaptureState.WaitingForSync1;
    private int _payloadLength;
    private int _payloadBytesRead;

    private enum CaptureState
    {
        WaitingForSync1,
        WaitingForSync2,
        WaitingForPacketType,
        WaitingForPayloadLength,
        ReadingPayload,
        WaitingForCrc
    }

    public int ValidPacketCount { get; private set; }

    public int InvalidPacketCount { get; private set; }

    public PacketParseResult? ProcessByte(byte incomingByte)
    {
        switch (_state)
        {
            case CaptureState.WaitingForSync1:
                ReadSync1(incomingByte);
                return null;

            case CaptureState.WaitingForSync2:
                ReadSync2(incomingByte);
                return null;

            case CaptureState.WaitingForPacketType:
                _buffer.Add(incomingByte);
                _state = CaptureState.WaitingForPayloadLength;
                return null;

            case CaptureState.WaitingForPayloadLength:
                _buffer.Add(incomingByte);
                _payloadLength = incomingByte;
                _payloadBytesRead = 0;
                _state = _payloadLength == 0
                    ? CaptureState.WaitingForCrc
                    : CaptureState.ReadingPayload;
                return null;

            case CaptureState.ReadingPayload:
                _buffer.Add(incomingByte);
                _payloadBytesRead++;

                if (_payloadBytesRead == _payloadLength)
                {
                    _state = CaptureState.WaitingForCrc;
                }

                return null;

            case CaptureState.WaitingForCrc:
                _buffer.Add(incomingByte);
                return ValidateBufferedPacket();

            default:
                ResetCapture();
                return null;
        }
    }

    public void Reset()
    {
        ValidPacketCount = 0;
        InvalidPacketCount = 0;
        ResetCapture();
    }

    private void ReadSync1(byte incomingByte)
    {
        if (incomingByte != ProtocolConstants.Sync1)
        {
            return;
        }

        _buffer.Clear();
        _buffer.Add(incomingByte);
        _state = CaptureState.WaitingForSync2;
    }

    private void ReadSync2(byte incomingByte)
    {
        if (incomingByte == ProtocolConstants.Sync2)
        {
            _buffer.Add(incomingByte);
            _state = CaptureState.WaitingForPacketType;
            return;
        }

        if (incomingByte == ProtocolConstants.Sync1)
        {
            _buffer.Clear();
            _buffer.Add(incomingByte);
            return;
        }

        ResetCapture();
    }

    private PacketParseResult ValidateBufferedPacket()
    {
        PacketParseResult result = PacketValidator.Validate(_buffer.ToArray());

        if (result.IsSuccess)
        {
            ValidPacketCount++;
        }
        else
        {
            InvalidPacketCount++;
        }

        ResetCapture();

        return result;
    }

    private void ResetCapture()
    {
        _buffer.Clear();
        _state = CaptureState.WaitingForSync1;
        _payloadLength = 0;
        _payloadBytesRead = 0;
    }
}
