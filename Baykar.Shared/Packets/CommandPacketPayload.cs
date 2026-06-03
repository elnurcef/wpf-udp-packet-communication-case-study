using Baykar.Shared.Enums;

namespace Baykar.Shared.Packets;

public readonly struct CommandPacketPayload
{
    public const int PayloadLength = 1;

    public CommandPacketPayload(CommandType command)
    {
        Command = command;
    }

    public CommandType Command { get; }

    public byte[] ToByteArray()
    {
        return [(byte)Command];
    }

    public static CommandPacketPayload FromByteArray(ReadOnlySpan<byte> data)
    {
        if (data.Length != PayloadLength)
        {
            throw new ArgumentException($"Input length must be {PayloadLength} byte.", nameof(data));
        }

        return new CommandPacketPayload((CommandType)data[0]);
    }
}
