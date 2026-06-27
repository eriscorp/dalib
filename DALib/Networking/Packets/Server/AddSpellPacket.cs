using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x17 (S->C) - place a spell in a spell-pane slot. Body: <c>[u8 Slot][u16 Icon]
///     [u8 UseType][string8 Name][string8 Prompt][u8 CastLines]</c>, fully consumed.
///     <see cref="Icon" /> is the spell-pane icon index, written raw (no 0x8000 offset, unlike item
///     sprites). <see cref="Prompt" /> is the text shown for prompt-type <see cref="UseType" />
///     values. The inverse operation is 0x18 <see cref="RemoveSpellPacket" />.
/// </summary>
[ServerOpcode(ServerOpcode.AddSpell)]
public sealed record AddSpellPacket : ServerPacket
{
    /// <summary>The spell-pane slot to place the spell in.</summary>
    public required byte Slot { get; init; }

    /// <summary>The spell-pane icon index (raw, no offset).</summary>
    public required ushort Icon { get; init; }

    /// <summary>How the spell is invoked (target / prompt / no input).</summary>
    public required SpellUseType UseType { get; init; }

    /// <summary>The spell's display name.</summary>
    public required string Name { get; init; }

    /// <summary>
    ///     The prompt text shown for prompt-type <see cref="UseType" /> values. Empty by default.
    /// </summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>The number of cast lines (chant duration) displayed for the spell.</summary>
    public required byte CastLines { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.AddSpell;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteUInt16(Icon);
        writer.WriteByte((byte)UseType);
        writer.WriteString8(Name);
        writer.WriteString8(Prompt);
        writer.WriteByte(CastLines);
    }

    /// <inheritdoc />
    public static AddSpellPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var icon = reader.ReadUInt16();
        var useType = (SpellUseType)reader.ReadByte();
        var name = reader.ReadString8();
        var prompt = reader.ReadString8();
        var castLines = reader.ReadByte();

        return new AddSpellPacket
        {
            Slot = slot,
            Icon = icon,
            UseType = useType,
            Name = name,
            Prompt = prompt,
            CastLines = castLines,
        };
    }
}
