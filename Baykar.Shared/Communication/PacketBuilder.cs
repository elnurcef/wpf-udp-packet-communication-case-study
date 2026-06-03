using Baykar.Shared.Crc;
using Baykar.Shared.Enums;

namespace Baykar.Shared.Communication;

public static class PacketBuilder
{
    private const int HeaderLength = 4;
    private const int CrcLength = 1;
    private const int MaximumPayloadLength = byte.MaxValue;

    public static byte[] Build(PacketType packetType, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Length > MaximumPayloadLength)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), "Payload length must not be greater than 255 bytes.");
        }

        byte payloadLength = (byte)payload.Length;
        byte[] packet = new byte[HeaderLength + payload.Length + CrcLength];

        packet[0] = ProtocolConstants.Sync1;
        packet[1] = ProtocolConstants.Sync2;
        packet[2] = (byte)packetType;
        packet[3] = payloadLength;

        payload.CopyTo(packet, HeaderLength);

        packet[^1] = Crc8Calculator.Calculate(packet.AsSpan(0, packet.Length - CrcLength));

        return packet;
    }
}
