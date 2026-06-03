using Baykar.Shared.Enums;
using Baykar.Shared.Packets;

namespace Baykar.Shared.Communication;

public static class PacketLengthProvider
{
    public static int GetExpectedPayloadLength(PacketType packetType)
    {
        return packetType switch
        {
            PacketType.CommunicationPacket1 => CommunicationPacket1Payload.PayloadLength,
            PacketType.CommunicationPacket2 => CommunicationPacket2Payload.PayloadLength,
            PacketType.SettingPacket => SettingPacketPayload.PayloadLength,
            PacketType.CommandPacket => CommandPacketPayload.PayloadLength,
            PacketType.FeedbackPacket => FeedbackPacketPayload.PayloadLength,
            _ => throw new ArgumentOutOfRangeException(nameof(packetType), "Unknown packet type.")
        };
    }
}
