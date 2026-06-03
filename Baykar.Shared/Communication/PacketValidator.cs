using Baykar.Shared.Crc;
using Baykar.Shared.Enums;
using Baykar.Shared.Models;

namespace Baykar.Shared.Communication;

public static class PacketValidator
{
    private const int MinimumPacketLength = 5;
    private const int HeaderLength = 4;
    private const int CrcLength = 1;

    public static PacketParseResult Validate(byte[] rawPacket)
    {
        if (rawPacket is null)
        {
            return PacketParseResult.Fail("Raw packet must not be null.");
        }

        if (rawPacket.Length < MinimumPacketLength)
        {
            return PacketParseResult.Fail("Raw packet length must be at least 5 bytes.");
        }

        if (rawPacket[0] != ProtocolConstants.Sync1)
        {
            return PacketParseResult.Fail("First sync byte is invalid.");
        }

        if (rawPacket[1] != ProtocolConstants.Sync2)
        {
            return PacketParseResult.Fail("Second sync byte is invalid.");
        }

        PacketType packetType = (PacketType)rawPacket[2];

        if (!Enum.IsDefined(packetType))
        {
            return PacketParseResult.Fail("Packet type is invalid.");
        }

        byte payloadLength = rawPacket[3];
        int expectedPayloadLength = PacketLengthProvider.GetExpectedPayloadLength(packetType);

        if (payloadLength != expectedPayloadLength)
        {
            return PacketParseResult.Fail($"Payload length is invalid. Expected {expectedPayloadLength}, received {payloadLength}.");
        }

        int expectedPacketLength = HeaderLength + payloadLength + CrcLength;

        if (rawPacket.Length != expectedPacketLength)
        {
            return PacketParseResult.Fail($"Raw packet length is invalid. Expected {expectedPacketLength}, received {rawPacket.Length}.");
        }

        byte expectedCrc = Crc8Calculator.Calculate(rawPacket.AsSpan(0, rawPacket.Length - CrcLength));
        byte receivedCrc = rawPacket[^1];

        if (receivedCrc != expectedCrc)
        {
            return PacketParseResult.Fail($"CRC is invalid. Expected {expectedCrc:X2}, received {receivedCrc:X2}.");
        }

        ParsedPacket parsedPacket = new(
            packetType,
            payloadLength,
            rawPacket.AsSpan(HeaderLength, payloadLength).ToArray(),
            receivedCrc,
            rawPacket.ToArray());

        return PacketParseResult.Success(parsedPacket);
    }
}
