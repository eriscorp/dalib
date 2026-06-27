using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x47 (C->S) - spend a level-up point to raise one primary stat. Body: <c>[u8 Stat]</c>
///     selecting which stat.
/// </summary>
/// <remarks>
///     <see cref="Stat" /> is a single-bit selector: <c>0x01</c> Str, <c>0x02</c> Dex, <c>0x04</c>
///     Int, <c>0x08</c> Wis, <c>0x10</c> Con; other values are ignored. The raise applies only when
///     the player has unspent level points.
/// </remarks>
[ClientOpcode(ClientOpcode.StatPoint)]
public sealed record StatPointPacket : ClientPacket
{
    /// <summary>
    ///     The stat to raise, as a single-bit selector: 0x01 Str, 0x02 Dex, 0x04 Int, 0x08 Wis,
    ///     0x10 Con.
    /// </summary>
    public required byte Stat { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.StatPoint;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Stat);

    /// <inheritdoc />
    public static StatPointPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new StatPointPacket
        {
            Stat = reader.ReadByte(),
        };
    }
}
