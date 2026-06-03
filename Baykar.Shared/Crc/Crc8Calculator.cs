namespace Baykar.Shared.Crc;

/// <summary>
/// Calculates checksums using the CRC-8/ATM algorithm.
/// </summary>
public static class Crc8Calculator
{
    private const byte Polynomial = 0x07;

    public static byte Calculate(ReadOnlySpan<byte> data)
    {
        byte crc = 0x00;

        foreach (byte value in data)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x80) != 0
                    ? (byte)((crc << 1) ^ Polynomial)
                    : (byte)(crc << 1);
            }
        }

        return crc;
    }
}
