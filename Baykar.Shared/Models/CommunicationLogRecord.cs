using Baykar.Shared.Enums;

namespace Baykar.Shared.Models;

public sealed class CommunicationLogRecord
{
    public DateTime TimestampUtc { get; init; }

    public PacketType PacketType { get; init; }

    public byte? Data1 { get; init; }

    public byte? Data2 { get; init; }

    public short? Data3 { get; init; }

    public int? Data4 { get; init; }

    public short? Data5 { get; init; }

    public float? Data6Float { get; init; }

    public float? Data7Float { get; init; }

    public uint? SystemTime { get; init; }

    public double? Data6Double { get; init; }
}
