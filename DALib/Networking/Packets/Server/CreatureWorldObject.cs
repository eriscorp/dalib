using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     Creature / NPC / player shape of <see cref="WorldObject" /> - 17 wire bytes
///     (18 + name length when <see cref="Type" /> equals <see cref="TypeNamed" />).
///     This shape applies to <see cref="WorldObject.Sprite" /> values in
///     <c>[0x4000, 0x8000)</c>.
/// </summary>
public sealed record CreatureWorldObject : WorldObject
{
    /// <summary>
    ///     When <see cref="Type" /> equals this value, the wire format carries a
    ///     trailing <see cref="Name" /> string and a name tag is rendered. Other
    ///     <see cref="Type" /> values omit the name.
    /// </summary>
    public const byte TypeNamed = 2;

    /// <summary>
    ///     First of four per-creature override bytes. Each slot's interpretation is
    ///     defined per-creature by the creature's MPF resource. Emitting 0 selects the
    ///     MPF default - a safe baseline.
    /// </summary>
    public byte Slot0 { get; set; }

    /// <summary>Second per-creature override byte. See <see cref="Slot0" />.</summary>
    public byte Slot1 { get; set; }

    /// <summary>Third per-creature override byte. See <see cref="Slot0" />.</summary>
    public byte Slot2 { get; set; }

    /// <summary>Fourth per-creature override byte. See <see cref="Slot0" />.</summary>
    public byte Slot3 { get; set; }

    /// <summary>
    ///     Creature-type classifier. Selects the minimap-dot palette index and gates the
    ///     <see cref="Name" /> string emit; <c>2</c> (<see cref="TypeNamed" />) carries a
    ///     name, other values do not.
    /// </summary>
    public byte Type { get; set; }

    /// <summary>
    ///     Name tag rendered above the creature. Only emitted on the wire when
    ///     <see cref="Type" /> equals <see cref="TypeNamed" />; ignored at write-time for
    ///     any other <see cref="Type" /> value (round-trip is lossy in that case - set
    ///     <see cref="Type" /> to <see cref="TypeNamed" /> if the name should travel).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    internal override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);
        writer.WriteUInt32(Id);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Slot0);
        writer.WriteByte(Slot1);
        writer.WriteByte(Slot2);
        writer.WriteByte(Slot3);
        writer.WriteByte(Direction);
        writer.WriteByte(Unknown);
        writer.WriteByte(Type);

        if (Type == TypeNamed)
            writer.WriteString8(Name);
    }
}
