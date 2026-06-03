using System.Buffers.Binary;
using Baykar.Shared.Enums;

namespace Baykar.Shared.Packets;

public readonly struct SettingPacketPayload
{
    public const int PayloadLength = 5;

    public SettingPacketPayload(SettingType setting, float value)
    {
        Setting = setting;
        Value = value;
    }

    public SettingType Setting { get; }

    public float Value { get; }

    public byte[] ToByteArray()
    {
        byte[] data = new byte[PayloadLength];

        data[0] = (byte)Setting;
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(1, sizeof(float)), Value);

        return data;
    }

    public static SettingPacketPayload FromByteArray(ReadOnlySpan<byte> data)
    {
        if (data.Length != PayloadLength)
        {
            throw new ArgumentException($"Input length must be {PayloadLength} bytes.", nameof(data));
        }

        return new SettingPacketPayload(
            (SettingType)data[0],
            BinaryPrimitives.ReadSingleLittleEndian(data.Slice(1, sizeof(float))));
    }
}
