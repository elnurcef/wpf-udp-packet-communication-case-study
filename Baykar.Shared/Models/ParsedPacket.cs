using Baykar.Shared.Enums;

namespace Baykar.Shared.Models;

public sealed class ParsedPacket
{
    public ParsedPacket(PacketType packetType, byte payloadLength, byte[] payload, byte crc, byte[] rawBytes)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(rawBytes);

        PacketType = packetType;
        PayloadLength = payloadLength;
        Payload = payload;
        Crc = crc;
        RawBytes = rawBytes;
    }

    public PacketType PacketType { get; }

    public byte PayloadLength { get; }

    public byte[] Payload { get; }

    public byte Crc { get; }

    public byte[] RawBytes { get; }
}
