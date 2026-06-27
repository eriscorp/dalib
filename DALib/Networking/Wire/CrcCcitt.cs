using System;

namespace DALib.Networking.Wire;

/// <summary>
///     CRC-16-CCITT, polynomial 0x1021, initial value 0x0000, no reflection, no XOR-out.
/// </summary>
/// <remarks>
///     Used by the 0x45 ByteHeartbeat (to checksum the two bytes carried by 0x3B) and by the
///     0x03 Login integrity trailer.
/// </remarks>
public static class CrcCcitt
{
    private static readonly ushort[] Table = BuildTable();

    /// <summary>
    ///     Update a running CRC with one data byte:
    ///     <c>crc' = table[(crc &gt;&gt; 8) &amp; 0xff] ^ (crc &lt;&lt; 8) ^ data</c>.
    /// </summary>
    public static ushort Step(ushort crc, byte data) =>
        (ushort)(Table[(crc >> 8) & 0xff] ^ (crc << 8) ^ data);

    /// <summary>
    ///     CRC-16-CCITT over a sequence of bytes.
    /// </summary>
    public static ushort Compute(ReadOnlySpan<byte> data, ushort initial = 0)
    {
        var crc = initial;

        foreach (var b in data)
            crc = Step(crc, b);

        return crc;
    }

    /// <summary>
    ///     The two-byte hash carried by 0x45 ByteHeartbeat. The two bytes from 0x3B are treated
    ///     as a big-endian uint16 (<c>val = (a &lt;&lt; 8) | b</c>); the LOW byte
    ///     (<paramref name="b" />) is fed first and the HIGH byte (<paramref name="a" />) second.
    /// </summary>
    public static ushort HeartbeatHash(byte a, byte b)
    {
        var crc = (ushort)0;
        crc = Step(crc, b);
        crc = Step(crc, a);

        return crc;
    }

    private static ushort[] BuildTable()
    {
        var t = new ushort[256];

        for (var i = 0; i < 256; i++)
        {
            var crc = (ushort)(i << 8);

            for (var j = 0; j < 8; j++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);

            t[i] = crc;
        }

        return t;
    }
}
