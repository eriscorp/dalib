using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x7A (C->S) - request the player's spouse/lover/family name. Pure trigger; carries no body. The
///     server answers with S->C 0x6D FamilyName.
/// </summary>
/// <remarks>
///     Binary-verified send (part of the retail marriage/family system); not emitted by Hybrasyl or Chaos.
///     Modeled for wire completeness.
/// </remarks>
[ClientOpcode(ClientOpcode.RequestLoverName)]
public sealed record RequestLoverNamePacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestLoverName;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body; the opcode alone requests the family name.
    }

    /// <inheritdoc />
    public static RequestLoverNamePacket Parse(ReadOnlySpan<byte> body) => new();
}
