using System;
using DALib.Definitions;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x06 (C->S) - request a one-tile walk in a cardinal direction. Body:
///     <c>[u8 direction][u8 sequence]</c>. The sequence byte is a per-connection walk counter,
///     incremented before each walk send; it is advisory and typically server-ignored. DALib leaves
///     the counter to the caller rather than coupling the packet to session state - set
///     <see cref="Sequence" /> from a counter you own.
/// </summary>
[ClientOpcode(ClientOpcode.Walk)]
public sealed record WalkPacket : ClientPacket
{
    /// <summary>The cardinal direction to step toward.</summary>
    public required Direction Direction { get; init; }

    /// <summary>
    ///     Per-connection walk sequence counter. Advisory and server-ignored; defaults to 0.
    ///     The caller owns the counter and increments it across walks.
    /// </summary>
    public byte Sequence { get; init; } = 0;

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Walk;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)Direction);
        writer.WriteByte(Sequence);
    }

    /// <inheritdoc />
    public static WalkPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var direction = (Direction)reader.ReadByte();
        var sequence = reader.ReadByte();

        return new WalkPacket
        {
            Direction = direction,
            Sequence = sequence,
        };
    }
}
