using System;
using System.Collections.Generic;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x2E (S->C) - opens the world-map (field-map) screen: a background image plus a set of
///     clickable nodes that warp the player. The body is
///     <c>[string8 FieldName][u8 NodeCount][u8 ImageIndex]</c> followed by <see cref="NodeCount" />
///     nodes (see <see cref="WorldMapNode" />). Clicking a node sends C->S 0x3F
///     <c>MapPointClick</c>.
/// </summary>
/// <remarks>
///     <para>
///         Each node renders either as a text label (at its wire X/Y) or as an image icon
///         (positioned from an asset table, ignoring the wire X/Y), depending on whether the node
///         text matches an entry in the image-asset table.
///     </para>
///     <para>
///         The four <c>u16</c>s after each node's text - <see cref="WorldMapNode.CheckSum" />,
///         <see cref="WorldMapNode.MapId" />, <see cref="WorldMapNode.DestinationX" />,
///         <see cref="WorldMapNode.DestinationY" /> - are opaque echo-back handles: they are
///         returned verbatim on the C->S 0x3F click and pair field-for-field with
///         <see cref="DALib.Networking.Packets.Client.MapPointClickPacket" />.
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.WorldMap)]
public sealed record WorldMapPacket : ServerPacket
{
    /// <summary>The background-image / field name selecting the world-map screen art.</summary>
    public required string FieldName { get; init; }

    /// <summary>
    ///     The image-asset index. Selects which entry of the image-asset table positions icon nodes.
    /// </summary>
    public required byte ImageIndex { get; init; }

    /// <summary>The clickable nodes. Wire-emits a u8 count (after the name) followed by each node.</summary>
    public IList<WorldMapNode> Nodes { get; set; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.WorldMap;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Nodes.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"WorldMap: node count {Nodes.Count} exceeds wire u8 limit ({byte.MaxValue}).");

        writer.WriteString8(FieldName);
        writer.WriteByte((byte)Nodes.Count);
        writer.WriteByte(ImageIndex);

        foreach (var node in Nodes)
        {
            writer.WriteUInt16(node.X);
            writer.WriteUInt16(node.Y);
            writer.WriteString8(node.Text);
            writer.WriteUInt16(node.CheckSum);
            writer.WriteUInt16(node.MapId);
            writer.WriteUInt16(node.DestinationX);
            writer.WriteUInt16(node.DestinationY);
        }
    }

    /// <inheritdoc />
    public static WorldMapPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var fieldName = reader.ReadString8();
        var nodeCount = reader.ReadByte();
        var imageIndex = reader.ReadByte();

        var nodes = new List<WorldMapNode>(nodeCount);

        for (var i = 0; i < nodeCount; i++)
        {
            var x = reader.ReadUInt16();
            var y = reader.ReadUInt16();
            var text = reader.ReadString8();
            var checkSum = reader.ReadUInt16();
            var mapId = reader.ReadUInt16();
            var destinationX = reader.ReadUInt16();
            var destinationY = reader.ReadUInt16();

            nodes.Add(new WorldMapNode
            {
                X = x,
                Y = y,
                Text = text,
                CheckSum = checkSum,
                MapId = mapId,
                DestinationX = destinationX,
                DestinationY = destinationY,
            });
        }

        return new WorldMapPacket
        {
            FieldName = fieldName,
            ImageIndex = imageIndex,
            Nodes = nodes,
        };
    }
}

/// <summary>
///     A single clickable world-map node carried by <see cref="WorldMapPacket" /> (0x2E). Wire
///     shape: <c>[u16 X][u16 Y][string8 Text][u16 CheckSum][u16 MapId][u16 DestinationX]
///     [u16 DestinationY]</c>. The trailing four <c>u16</c>s are opaque echo handles returned
///     verbatim on the C->S 0x3F click - see <see cref="WorldMapPacket" /> remarks.
/// </summary>
public sealed record WorldMapNode
{
    /// <summary>The node's X position on the world-map image (text nodes only; icon nodes are positioned from the asset table).</summary>
    public required ushort X { get; init; }

    /// <summary>The node's Y position on the world-map image (text nodes only).</summary>
    public required ushort Y { get; init; }

    /// <summary>The node's label / asset-name (matched against the image-asset table to pick text vs icon).</summary>
    public required string Text { get; init; }

    /// <summary>Echo handle #1 - the destination map's checksum. Returned verbatim on 0x3F click.</summary>
    public required ushort CheckSum { get; init; }

    /// <summary>Echo handle #2 - the destination map id. Returned verbatim on 0x3F click.</summary>
    public required ushort MapId { get; init; }

    /// <summary>Echo handle #3 - the destination X. Returned verbatim on 0x3F click.</summary>
    public required ushort DestinationX { get; init; }

    /// <summary>Echo handle #4 - the destination Y. Returned verbatim on 0x3F click.</summary>
    public required ushort DestinationY { get; init; }
}
