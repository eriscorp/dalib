using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x49 (S->C) - request the player's portrait and profile-text upload. A payload-free signal;
///     the response is C->S 0x4F SetProfile. The body bytes are inert and carry no meaning.
///     Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
[ServerOpcode(ServerOpcode.RequestPortrait)]
public sealed record RequestPortraitPacket : ServerPacket
{
    /// <summary>
    ///     Optional trailing bytes, preserved for byte-faithful round-trips; defaults to two zero bytes.
    ///     Carries no meaning.
    /// </summary>
    public byte[] Padding { get; set; } = [0x00, 0x00];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.RequestPortrait;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteBytes(Padding);

    /// <inheritdoc />
    public static RequestPortraitPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var padding = reader.Remaining.ToArray();

        return new RequestPortraitPacket
        {
            Padding = padding,
        };
    }
}
