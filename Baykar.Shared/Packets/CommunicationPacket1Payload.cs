using System.Buffers.Binary;

namespace Baykar.Shared.Packets;

public readonly struct CommunicationPacket1Payload
{
    public const int PayloadLength = 15;

    public CommunicationPacket1Payload(
        byte data1,
        byte data2,
        byte data3,
        short data4,
        short data5,
        float data6,
        float data7)
    {
        Data1 = data1;
        Data2 = data2;
        Data3 = data3;
        Data4 = data4;
        Data5 = data5;
        Data6 = data6;
        Data7 = data7;
    }

    public byte Data1 { get; }

    public byte Data2 { get; }

    public byte Data3 { get; }

    public short Data4 { get; }

    // The PDF sample lists Data5 as INT16 but gives 72635, which is outside the Int16 range.
    public short Data5 { get; }

    public float Data6 { get; }

    public float Data7 { get; }

    public byte[] ToByteArray()
    {
        byte[] data = new byte[PayloadLength];

        data[0] = Data1;
        data[1] = Data2;
        data[2] = Data3;
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(3, sizeof(short)), Data4);
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(5, sizeof(short)), Data5);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(7, sizeof(float)), Data6);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(11, sizeof(float)), Data7);

        return data;
    }

    public static CommunicationPacket1Payload FromByteArray(ReadOnlySpan<byte> data)
    {
        if (data.Length != PayloadLength)
        {
            throw new ArgumentException($"Input length must be {PayloadLength} bytes.", nameof(data));
        }

        return new CommunicationPacket1Payload(
            data[0],
            data[1],
            data[2],
            BinaryPrimitives.ReadInt16LittleEndian(data.Slice(3, sizeof(short))),
            BinaryPrimitives.ReadInt16LittleEndian(data.Slice(5, sizeof(short))),
            BinaryPrimitives.ReadSingleLittleEndian(data.Slice(7, sizeof(float))),
            BinaryPrimitives.ReadSingleLittleEndian(data.Slice(11, sizeof(float))));
    }
}
