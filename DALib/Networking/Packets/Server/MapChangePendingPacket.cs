using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x67 (S->C) - the "map change pending" signal put on the wire at the start of a map transition,
///     just before the new <see cref="MapInfoPacket" /> (0x15) and <see cref="LocationPacket" /> (0x04).
///     The body is a short fixed payload with no decoded fields.
/// </summary>
/// <remarks>
///     Modeled for protocol completeness. The body has no recoverable field semantics, so it is modeled
///     like 0x22 <see cref="RefreshPacket" />: a settable <see cref="Payload" /> that emits a byte-faithful
///     body and tolerates any length on parse.
/// </remarks>
[ServerOpcode(ServerOpcode.MapChangePending)]
public sealed record MapChangePendingPacket : ServerPacket
{
    /// <summary>
    ///     The fixed payload put on the wire. It carries no decoded meaning; settable for byte-faithful
    ///     round-trips and defaulted to the common 6-byte body (<c>[0x03 0x00 0x00 0x00 0x00 0x00]</c>).
    /// </summary>
    public byte[] Payload { get; set; } = [0x03, 0x00, 0x00, 0x00, 0x00, 0x00];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.MapChangePending;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteBytes(Payload);

    /// <inheritdoc />
    public static MapChangePendingPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var payload = reader.Remaining.ToArray();

        return new MapChangePendingPacket
        {
            Payload = payload,
        };
    }
}
