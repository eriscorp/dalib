using System;
using System.Collections.Generic;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x06 (S->C) - a runtime map-tile patch. The body is a rectangle header
///     <c>[u8 StartX][u8 StartY][u8 Width][u8 Height]</c> followed by <c>Width x Height</c> tile cells,
///     each three big-endian <c>u16</c>s (<c>[Background][LeftForeground][RightForeground]</c>), in
///     row-major order (outer Y, inner X).
/// </summary>
/// <remarks>
///     Modeled for protocol completeness; not emitted by typical servers. Live-patches a rectangular block
///     of the current map (a terrain/object edit pushed mid-session). The three <c>u16</c>s per cell match
///     <see cref="DALib.Data.MapTile" />'s layers.
/// </remarks>
[ServerOpcode(ServerOpcode.MapEdit)]
public sealed record MapEditPacket : ServerPacket
{
    /// <summary>The left edge (X) of the patched rectangle, in tile coordinates.</summary>
    public required byte StartX { get; init; }

    /// <summary>The top edge (Y) of the patched rectangle, in tile coordinates.</summary>
    public required byte StartY { get; init; }

    /// <summary>The width of the patched rectangle, in tiles.</summary>
    public required byte Width { get; init; }

    /// <summary>The height of the patched rectangle, in tiles.</summary>
    public required byte Height { get; init; }

    /// <summary>
    ///     The <c>Width x Height</c> tile cells, row-major (outer Y, inner X). Each cell carries the three
    ///     tile layers (Background / LeftForeground / RightForeground).
    /// </summary>
    public IList<MapEditTile> Tiles { get; init; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.MapEdit;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        var expected = Width * Height;

        if (Tiles.Count != expected)
            throw new InvalidOperationException(
                $"MapEditPacket: Tiles.Count ({Tiles.Count}) must equal Width x Height ({expected}).");

        writer.WriteByte(StartX);
        writer.WriteByte(StartY);
        writer.WriteByte(Width);
        writer.WriteByte(Height);

        foreach (var tile in Tiles)
        {
            writer.WriteUInt16(tile.Background);
            writer.WriteUInt16(tile.LeftForeground);
            writer.WriteUInt16(tile.RightForeground);
        }
    }

    /// <inheritdoc />
    public static MapEditPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var startX = reader.ReadByte();
        var startY = reader.ReadByte();
        var width = reader.ReadByte();
        var height = reader.ReadByte();

        var count = width * height;
        var tiles = new List<MapEditTile>(count);

        for (var i = 0; i < count; i++)
            tiles.Add(new MapEditTile
            {
                Background = reader.ReadUInt16(),
                LeftForeground = reader.ReadUInt16(),
                RightForeground = reader.ReadUInt16(),
            });

        return new MapEditPacket
        {
            StartX = startX,
            StartY = startY,
            Width = width,
            Height = height,
            Tiles = tiles,
        };
    }
}

/// <summary>
///     One tile cell of a <see cref="MapEditPacket" /> - the three map layers (each a big-endian <c>u16</c>
///     tile id, matching <see cref="DALib.Data.MapTile" />).
/// </summary>
public sealed record MapEditTile
{
    /// <summary>The background (ground) tile id.</summary>
    public required ushort Background { get; init; }

    /// <summary>The left-foreground (object) tile id.</summary>
    public required ushort LeftForeground { get; init; }

    /// <summary>The right-foreground (object) tile id.</summary>
    public required ushort RightForeground { get; init; }
}
