using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x4E (C-&gt;S) - one line of a spell's chant, sent as the spell is cast. A cast emits a
///     sequence of these (one <see cref="Line" /> per packet) between the 0x4D BeginCasting that
///     opens the cast and the 0x0F UseSpell that executes it. Body: <c>[string8 line]</c>.
/// </summary>
/// <remarks>
///     The server broadcasts each line to nearby clients as spell-coloured chat (S-&gt;C 0x0D with
///     chat type 2), which is how onlookers see the chant appear word by word. The chant's lead-in
///     words and the spell's actual chant are sent as separate lines; this packet models one of them.
/// </remarks>
[ClientOpcode(ClientOpcode.CastLine)]
public sealed record CastLinePacket : ClientPacket
{
    /// <summary>One line of the chant being spoken.</summary>
    public required string Line { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.CastLine;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteString8(Line);

    /// <inheritdoc />
    public static CastLinePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new CastLinePacket
        {
            Line = reader.ReadString8(),
        };
    }
}
