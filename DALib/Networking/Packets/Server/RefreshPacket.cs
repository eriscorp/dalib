using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x22 (S->C) - a payload-free "refresh the view" signal that nudges a redraw of the current
///     state (commonly sent after warps and similar transitions). No structured body is read.
/// </summary>
/// <remarks>
///     Modeled with a settable <see cref="Padding" /> (default single zero) for byte-faithful
///     round-trips; <see cref="Parse" /> tolerates any body length.
/// </remarks>
[ServerOpcode(ServerOpcode.Refresh)]
public sealed record RefreshPacket : ServerPacket
{
    /// <summary>
    ///     Optional ceremonial body; carries no meaning. Settable for byte-faithful round-trips;
    ///     defaults to a single zero byte.
    /// </summary>
    public byte[] Padding { get; set; } = [0x00];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Refresh;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteBytes(Padding);

    /// <inheritdoc />
    public static RefreshPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var padding = reader.Remaining.ToArray();

        return new RefreshPacket
        {
            Padding = padding,
        };
    }
}
