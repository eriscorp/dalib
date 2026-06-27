using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x44 (S->C) - a payload-free signal that carries no body. Modeled for protocol completeness;
///     not emitted by typical servers. <see cref="WriteBody" /> writes nothing and
///     <see cref="Parse" /> tolerates and discards any trailing bytes.
/// </summary>
[ServerOpcode(ServerOpcode.AddUser)]
public sealed record AddUserPacket : ServerPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.AddUser;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) { }

    /// <inheritdoc />
    public static AddUserPacket Parse(ReadOnlySpan<byte> body) => new();
}
