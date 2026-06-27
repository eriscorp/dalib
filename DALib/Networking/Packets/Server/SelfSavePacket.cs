using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x21 (S->C) - a payload-free self-save acknowledgment ("your character was saved"); on receipt
///     a "Saved." message popup is shown. Carries no body; <see cref="WriteBody" /> writes nothing and
///     <see cref="Parse" /> tolerates and discards any trailing bytes.
///     Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
[ServerOpcode(ServerOpcode.SelfSave)]
public sealed record SelfSavePacket : ServerPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.SelfSave;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) { }

    /// <inheritdoc />
    public static SelfSavePacket Parse(ReadOnlySpan<byte> body) => new();
}
