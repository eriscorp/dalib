using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x13 (C-&gt;S) - request a basic "assail" attack in the player's current facing direction.
///     A pure trigger: the opcode alone, no body. The attack direction is the player's current
///     facing, which the server already tracks.
/// </summary>
[ClientOpcode(ClientOpcode.Attack)]
public sealed record AttackPacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Attack;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body - the opcode alone triggers the attack.
    }

    /// <inheritdoc />
    public static AttackPacket Parse(ReadOnlySpan<byte> body) => new();
}
