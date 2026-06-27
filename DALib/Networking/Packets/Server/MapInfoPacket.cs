using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x15 (S->C) - map header sent on every map entry: id, dimensions, weather/flags, a cache
///     checksum, and the map name. The body is <c>[u16 MapId][u8 Width][u8 Height][u8 Flags]
///     [u16 Reserved][u16 Checksum][string8 Name]</c>.
/// </summary>
/// <remarks>
///     <see cref="Flags" /> is a weather/display bitmask: bit 0 (0x01) snow, bit 1 (0x02) rain, both = dark,
///     bit 6 (0x40) no-map, bit 7 (0x80) snow. Modeled as the raw byte. The <see cref="Checksum" /> is
///     written big-endian per this library's <c>WriteUInt16</c> convention; since maps are cached by their
///     raw received bytes, byte order is not observable as long as a given server is internally consistent.
/// </remarks>
[ServerOpcode(ServerOpcode.MapInfo)]
public sealed record MapInfoPacket : ServerPacket
{
    /// <summary>The map's id.</summary>
    public required ushort MapId { get; init; }

    /// <summary>The map width in tiles.</summary>
    public required byte Width { get; init; }

    /// <summary>The map height in tiles.</summary>
    public required byte Height { get; init; }

    /// <summary>Weather / display bitmask (snow/rain/dark/no-map); see the remarks for the bits.</summary>
    public required byte Flags { get; init; }

    /// <summary>The map cache checksum; see the remarks on byte order.</summary>
    public required ushort Checksum { get; init; }

    /// <summary>The map's display name.</summary>
    public required string Name { get; init; }

    /// <summary>
    ///     The two reserved zero bytes between <see cref="Flags" /> and <see cref="Checksum" />. Settable
    ///     round-trip field; defaults to 0.
    /// </summary>
    public ushort Reserved { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.MapInfo;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(MapId);
        writer.WriteByte(Width);
        writer.WriteByte(Height);
        writer.WriteByte(Flags);
        writer.WriteUInt16(Reserved);
        writer.WriteUInt16(Checksum);
        writer.WriteString8(Name);
    }

    /// <inheritdoc />
    public static MapInfoPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var mapId = reader.ReadUInt16();
        var width = reader.ReadByte();
        var height = reader.ReadByte();
        var flags = reader.ReadByte();
        var reserved = reader.ReadUInt16();
        var checksum = reader.ReadUInt16();
        var name = reader.ReadString8();

        return new MapInfoPacket
        {
            MapId = mapId,
            Width = width,
            Height = height,
            Flags = flags,
            Checksum = checksum,
            Name = name,
            Reserved = reserved,
        };
    }
}
