using System.Buffers.Binary;

namespace Baykar.Shared.Packets;

public readonly struct CommunicationPacket2Payload
{
    public const int PayloadLength = 20;

    public CommunicationPacket2Payload(
        byte data1,
        byte data2,
        short data3,
        int data4,
        uint systemTime,
        double data6)
    {
        Data1 = data1;
        Data2 = data2;
        Data3 = data3;
        Data4 = data4;
        SystemTime = systemTime;
        Data6 = data6;
    }

    public byte Data1 { get; }

    public byte Data2 { get; }

    public short Data3 { get; }

    public int Data4 { get; }

    public uint SystemTime { get; }

    public double Data6 { get; }

    public byte[] ToByteArray()
    {
        byte[] data = new byte[PayloadLength];

        data[0] = Data1;
        data[1] = Data2;
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(2, sizeof(short)), Data3);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4, sizeof(int)), Data4);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, sizeof(uint)), SystemTime);
        BinaryPrimitives.WriteDoubleLittleEndian(data.AsSpan(12, sizeof(double)), Data6);

        return data;
    }

    public static CommunicationPacket2Payload FromByteArray(ReadOnlySpan<byte> data)
    {
        if (data.Length != PayloadLength)
        {
            throw new ArgumentException($"Input length must be {PayloadLength} bytes.", nameof(data));
        }

        return new CommunicationPacket2Payload(
            data[0],
            data[1],
            BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2, sizeof(short))),
            BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4, sizeof(int))),
            BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(8, sizeof(uint))),
            BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(12, sizeof(double))));
    }
}
