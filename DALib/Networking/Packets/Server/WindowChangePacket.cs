using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x3E (S->C) - a one-byte window-change signal. The body is a single <c>[u8]</c>.
/// </summary>
/// <remarks>
///     Switches a UI window / panel to a server-selected mode. Values 0-4 select a mode; values
///     outside 0-4 are no-ops. Modeled for protocol completeness; not emitted by typical servers.
///     The exact UI meaning of the mode codes is not modeled - <see cref="Value" /> is preserved
///     verbatim for round-tripping.
/// </remarks>
[ServerOpcode(ServerOpcode.WindowChange)]
public sealed record WindowChangePacket : ServerPacket
{
    /// <summary>The single body byte. Preserved verbatim.</summary>
    public required byte Value { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.WindowChange;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Value);

    /// <inheritdoc />
    public static WindowChangePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new WindowChangePacket
        {
            Value = reader.ReadByte(),
        };
    }
}
