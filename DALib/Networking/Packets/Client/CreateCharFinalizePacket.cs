using System;
using DALib.Enums;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x04 (C->S) - the second half of character creation: appearance for the name reserved by a
///     prior <see cref="CreateCharRequestPacket" /> (0x02) on the same connection, sent after the
///     0x02 success response. Wire body (3 bytes): <c>[u8 HairStyle][u8 Gender][u8 HairColor]</c>.
/// </summary>
/// <remarks>
///     <see cref="Gender" /> is <c>1 = Male</c>, <c>2 = Female</c> on the wire; creation only ever
///     emits those two. It is modeled as the categorical <see cref="DALib.Enums.Gender" />
///     enum (which also carries <c>Neutral = 0</c>, the genderless default used elsewhere) to keep
///     arbitrary byte values off the wire. <see cref="HairStyle" /> and <see cref="HairColor" /> are
///     sprite-table indices, so they stay bytes. Values are written faithfully and not clamped; the
///     server clamps on receipt (hairStyle [1,17], hairColor [0,13], gender [1,2]), silently
///     adjusting out-of-range values rather than rejecting them.
/// </remarks>
[ClientOpcode(ClientOpcode.CreateCharFinalize)]
public sealed record CreateCharFinalizePacket : ClientPacket
{
    /// <summary>Hair style sprite index (server clamps to [1,17]).</summary>
    public required byte HairStyle { get; init; }

    /// <summary>Character gender. Creation emits Male or Female; the server clamps to [1,2].</summary>
    public required Gender Gender { get; init; }

    /// <summary>Hair colour palette index (server clamps to [0,13]).</summary>
    public required byte HairColor { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.CreateCharFinalize;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(HairStyle);
        writer.WriteByte((byte)Gender);
        writer.WriteByte(HairColor);
    }

    /// <inheritdoc />
    public static CreateCharFinalizePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new CreateCharFinalizePacket
        {
            HairStyle = reader.ReadByte(),
            Gender = (Gender)reader.ReadByte(),
            HairColor = reader.ReadByte(),
        };
    }
}
