using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x05 (C->S) - ask the server to (re)send the current map's tile rows. Sent when the CRC of the
///     locally-cached map data does <em>not</em> match the CRC the server advertised in the preceding
///     0x15 MapInfo. A match suppresses the send; a mismatch sends this packet carrying the cached CRC,
///     and the server replies with 0x3C tile rows. Some servers read zero bytes and emit all tile rows
///     unconditionally, so this body may be inert; it is modeled in full for wire-accuracy.
/// </summary>
/// <remarks>
///     The CRC (<see cref="CachedCrc" />) is a CRC-CCITT over the currently-loaded map tile grid - every
///     6-byte cell (three u16 layers: background plus two foreground) across <see cref="X" /> x
///     <see cref="Y" />, the same CrcCcitt DALib uses elsewhere. On the wire it occupies a 24-bit
///     big-endian field, but the value is always a promoted u16 (high byte zero), so it is modeled here
///     as a <see cref="ushort" />. The map number is absent (the server knows the current map from
///     session state); the two leading u16 fields are reserved and always zero, written as constants.
/// </remarks>
[ClientOpcode(ClientOpcode.RequestMap)]
public sealed record RequestMapPacket : ClientPacket
{
    /// <summary>Map width in tiles (as reported by the preceding 0x15 MapInfo).</summary>
    public required byte X { get; init; }

    /// <summary>Map height in tiles (as reported by the preceding 0x15 MapInfo).</summary>
    public required byte Y { get; init; }

    /// <summary>
    ///     CRC-CCITT of the currently-cached map tile data, compared by the server against the CRC it
    ///     advertised in 0x15 to decide whether a refresh is needed. Defaults to <c>0</c>, the
    ///     "no cached map" state: with no cached tile data for the map, send this packet with CRC 0 to
    ///     request the full map.
    /// </summary>
    public ushort CachedCrc { get; init; } = 0;

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestMap;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(0); // reserved
        writer.WriteUInt16(0); // reserved
        writer.WriteByte(X);
        writer.WriteByte(Y);
        writer.WriteByte(0); // u24-BE CRC high byte - always zero (promoted u16)
        writer.WriteUInt16(CachedCrc);
    }

    /// <inheritdoc />
    public static RequestMapPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        reader.ReadUInt16(); // reserved
        reader.ReadUInt16(); // reserved
        var x = reader.ReadByte();
        var y = reader.ReadByte();
        reader.ReadByte(); // u24-BE CRC high byte (always zero)
        var cachedCrc = reader.ReadUInt16();

        return new RequestMapPacket
        {
            X = x,
            Y = y,
            CachedCrc = cachedCrc,
        };
    }
}
