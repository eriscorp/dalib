using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x4A (S->C) - body <c>[u8 Type][u8 Payload][u32 Magic]</c> (the <c>u32</c> is big-endian).
///     Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
[ServerOpcode(ServerOpcode.BadGuy)]
public sealed record BadGuyPacket : ServerPacket
{
    /// <summary>A fixed magic constant associated with this opcode.</summary>
    public const uint MagicValue = 0x7D3AFF99;

    /// <summary>The first body byte.</summary>
    public required byte Type { get; init; }

    /// <summary>A single tag byte.</summary>
    public required byte Payload { get; init; }

    /// <summary>Big-endian <c>u32</c> magic value.</summary>
    public required uint Magic { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.BadGuy;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteByte(Payload);
        writer.WriteUInt32(Magic);
    }

    /// <inheritdoc />
    public static BadGuyPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new BadGuyPacket
        {
            Type = reader.ReadByte(),
            Payload = reader.ReadByte(),
            Magic = reader.ReadUInt32(),
        };
    }
}
