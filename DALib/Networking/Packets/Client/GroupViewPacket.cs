using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x46 (C->S) - request the details of the current group (the group-view window). Pure trigger;
///     carries no body.
/// </summary>
/// <remarks>
///     Client-supported (binary-verified send) but not emitted by Hybrasyl or Chaos - a LoD-lineage
///     opcode modeled for wire completeness.
/// </remarks>
[ClientOpcode(ClientOpcode.GroupView)]
public sealed record GroupViewPacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.GroupView;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body; the opcode alone requests the group details.
    }

    /// <inheritdoc />
    public static GroupViewPacket Parse(ReadOnlySpan<byte> body) => new();
}
