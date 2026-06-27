using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x1D (C->S) - play an emote (a body animation) on the player. Body: a single
///     <c>[u8 EmoteIndex]</c> in the range 0-35. The wire byte is the zero-based emote index, not
///     the body-animation id: it maps to body animation <c>EmoteIndex + 9</c> (i.e. 9-44). Modeled
///     as the raw wire index; mapping to a named animation is left to the caller.
/// </summary>
[ClientOpcode(ClientOpcode.Emote)]
public sealed record EmotePacket : ClientPacket
{
    /// <summary>
    ///     The zero-based emote index (0-35) as carried on the wire. Maps to body animation
    ///     <c>EmoteIndex + 9</c>.
    /// </summary>
    public required byte EmoteIndex { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Emote;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(EmoteIndex);

    /// <inheritdoc />
    public static EmotePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new EmotePacket
        {
            EmoteIndex = reader.ReadByte(),
        };
    }
}
