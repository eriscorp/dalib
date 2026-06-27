using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x3F (C->S) - a click on a node of the world map (the field-map screen opened by the S->C
///     0x2E FieldMap). Body: <c>[u16 BE CheckSum][u16 BE MapId][u16 BE X][u16 BE Y]</c> (8 bytes).
///     The four values are opaque round-trip handles echoed back verbatim from the FieldMap point;
///     the names (CheckSum/MapId/X/Y) describe their conventional reading, not an assigned meaning.
/// </summary>
[ClientOpcode(ClientOpcode.MapPointClick)]
public sealed record MapPointClickPacket : ClientPacket
{
    /// <summary>The clicked point's checksum handle; first handle on the wire.</summary>
    public required ushort CheckSum { get; init; }

    /// <summary>The clicked point's map-id handle; second handle on the wire.</summary>
    public required ushort MapId { get; init; }

    /// <summary>The clicked point's X handle; third handle on the wire.</summary>
    public required ushort X { get; init; }

    /// <summary>The clicked point's Y handle; fourth handle on the wire.</summary>
    public required ushort Y { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.MapPointClick;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(CheckSum);
        writer.WriteUInt16(MapId);
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);
    }

    /// <inheritdoc />
    public static MapPointClickPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var checkSum = reader.ReadUInt16();
        var mapId = reader.ReadUInt16();
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();

        return new MapPointClickPacket
        {
            CheckSum = checkSum,
            MapId = mapId,
            X = x,
            Y = y,
        };
    }
}
