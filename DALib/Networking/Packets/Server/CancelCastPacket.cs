using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x48 (S->C) - abort the spell currently being cast (clears the cast bar). A payload-free signal;
///     no body bytes are read.
/// </summary>
/// <remarks>
///     Emitters differ only in trailing slack (none, or a single <c>0x00</c>); none of it is read.
///     Modeled with a settable <see cref="Padding" /> for byte-faithful round-trips, and parsing
///     tolerates any length.
/// </remarks>
[ServerOpcode(ServerOpcode.CancelCast)]
public sealed record CancelCastPacket : ServerPacket
{
    /// <summary>
    ///     Trailing slack byte(s); carries no meaning and is not read. Settable for byte-faithful
    ///     round-trips; defaults to a single zero.
    /// </summary>
    public byte[] Padding { get; set; } = [0x00];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.CancelCast;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteBytes(Padding);

    /// <inheritdoc />
    public static CancelCastPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var padding = reader.Remaining.ToArray();

        return new CancelCastPacket
        {
            Padding = padding,
        };
    }
}
