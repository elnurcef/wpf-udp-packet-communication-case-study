namespace Baykar.Shared.Models;

public sealed class PacketParseResult
{
    private PacketParseResult(bool isSuccess, ParsedPacket? packet, string errorMessage)
    {
        IsSuccess = isSuccess;
        Packet = packet;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public ParsedPacket? Packet { get; }

    public string ErrorMessage { get; }

    public static PacketParseResult Success(ParsedPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        return new PacketParseResult(true, packet, string.Empty);
    }

    public static PacketParseResult Fail(string errorMessage)
    {
        return new PacketParseResult(false, null, errorMessage);
    }
}
