using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x58 (S->C) - signals that the map stream (0x15 header + 0x3C rows) is complete and the map may be
///     shown. A payload-free completion signal; the body is a single zero <c>u16</c>.
/// </summary>
/// <remarks>
///     A completion signal carrying no data - servers send only zero bytes (one or two). Modeled with a
///     settable <see cref="Padding" /> defaulting to a <c>u16</c> zero; parsing tolerates either length
///     (the value is meaningless).
/// </remarks>
[ServerOpcode(ServerOpcode.MapLoadComplete)]
public sealed record MapLoadCompletePacket : ServerPacket
{
    /// <summary>
    ///     The trailing zero byte(s) (one or two). Carries no meaning; settable for byte-faithful
    ///     round-trips. Defaults to a <c>u16 0</c> shape.
    /// </summary>
    public byte[] Padding { get; set; } = [0x00, 0x00];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.MapLoadComplete;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteBytes(Padding);

    /// <inheritdoc />
    public static MapLoadCompletePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var padding = reader.Remaining.ToArray();

        return new MapLoadCompletePacket
        {
            Padding = padding,
        };
    }
}
