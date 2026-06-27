using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x4D (C-&gt;S) - open a spell cast, ahead of the per-line 0x4E CastLine packets and the
///     0x0F UseSpell that executes it. Body: <c>[u8 lineCount]</c>, the number of chant lines the
///     spell has (its "Lines" stat). The spell's final incantation is the cast itself and is not
///     counted: a four-line spell sends <c>0x04</c> here and then five 0x4E CastLines (four chant
///     lines plus the incantation).
/// </summary>
[ClientOpcode(ClientOpcode.BeginCasting)]
public sealed record BeginCastingPacket : ClientPacket
{
    /// <summary>The spell's chant-line count (its "Lines" stat); the final incantation is not counted.</summary>
    public required byte Lines { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.BeginCasting;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Lines);

    /// <inheritdoc />
    public static BeginCastingPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new BeginCastingPacket
        {
            Lines = reader.ReadByte(),
        };
    }
}
