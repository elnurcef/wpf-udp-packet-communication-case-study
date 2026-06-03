using System.Buffers.Binary;

namespace Baykar.Shared.Packets;

public readonly struct FeedbackPacketPayload
{
    public const int PayloadLength = 5;

    public FeedbackPacketPayload(byte responseType, float value)
    {
        ResponseType = responseType;
        Value = value;
    }

    public byte ResponseType { get; }

    public float Value { get; }

    public byte[] ToByteArray()
    {
        byte[] data = new byte[PayloadLength];

        data[0] = ResponseType;
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(1, sizeof(float)), Value);

        return data;
    }

    public static FeedbackPacketPayload FromByteArray(ReadOnlySpan<byte> data)
    {
        if (data.Length != PayloadLength)
        {
            throw new ArgumentException($"Input length must be {PayloadLength} bytes.", nameof(data));
        }

        return new FeedbackPacketPayload(
            data[0],
            BinaryPrimitives.ReadSingleLittleEndian(data.Slice(1, sizeof(float))));
    }
}
