using System;
using DALib.Definitions;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x11 (C->S) - turn the player to face a cardinal direction, without moving.
///     Body: <c>[u8 direction]</c>. Unlike <see cref="WalkPacket" />, Turn carries no
///     sequence byte. Direction values above 3 are ignored.
/// </summary>
[ClientOpcode(ClientOpcode.Turn)]
public sealed record TurnPacket : ClientPacket
{
    /// <summary>The cardinal direction to face.</summary>
    public required Direction Direction { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Turn;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte((byte)Direction);

    /// <inheritdoc />
    public static TurnPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TurnPacket
        {
            Direction = (Direction)reader.ReadByte(),
        };
    }
}
