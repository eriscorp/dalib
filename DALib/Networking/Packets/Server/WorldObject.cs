using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     Abstract base for one renderable object in a <see cref="DrawObjectsPacket" />
///     (S->C 0x07). Concrete variants are <see cref="ItemWorldObject" /> (item, gold,
///     and out-of-render-range shapes - 13 wire bytes) and
///     <see cref="CreatureWorldObject" /> (NPCs, monsters, players - 17+ wire bytes).
/// </summary>
/// <remarks>
///     <para>
///         On parse, the variant is selected by <see cref="Sprite" /> range, not by a
///         discriminator byte:
///         <list type="bullet">
///             <item><c>sprite in [0x4000, 0x8000)</c> -> <see cref="CreatureWorldObject" /></item>
///             <item>everything else -> <see cref="ItemWorldObject" />, though only
///             <c>sprite in [0x8000, 0xC000)</c> renders (item / gold). Ranges
///             <c>[0x0000, 0x4000)</c> and <c>[0xC000, 0xFFFF]</c> parse as item-shape
///             but do not render.</item>
///         </list>
///         The variant subtypes structurally enforce the byte-count contract:
///         <see cref="ItemWorldObject" /> always emits exactly 13 bytes,
///         <see cref="CreatureWorldObject" /> always emits 17 (or 18 + name length
///         when <see cref="CreatureWorldObject.Type" /> is
///         <see cref="CreatureWorldObject.TypeNamed" />).
///     </para>
/// </remarks>
public abstract record WorldObject
{
    /// <summary>Tile X coordinate.</summary>
    public required ushort X { get; set; }

    /// <summary>Tile Y coordinate.</summary>
    public required ushort Y { get; set; }

    /// <summary>World-object id (server-assigned).</summary>
    public required uint Id { get; set; }

    /// <summary>
    ///     Sprite identifier. Determines which subtype the wire format
    ///     uses on <em>parse</em>; on <em>write</em>, the subtype determines the
    ///     bytes emitted regardless of this value. <c>[0x4000, 0x8000)</c> renders
    ///     as a creature; <c>[0x8000, 0xC000)</c> renders as an item or gold pile;
    ///     other ranges parse but do not render.
    /// </summary>
    public ushort Sprite { get; set; }

    /// <summary>
    ///     Facing direction. Wire-significant for creatures (drives sprite
    ///     orientation); meaningless for items (a dropped item is rendered flat
    ///     on the ground with no rotation) but still parsed/emitted by the wire
    ///     format, so present on the base record. Emit 0 on items.
    /// </summary>
    public byte Direction { get; set; }

    /// <summary>
    ///     Per-object trailing byte present on both variants; no known consumer. Emit 0;
    ///     preserved for round-trip fidelity.
    /// </summary>
    public byte Unknown { get; set; }

    internal abstract void WriteBody(IPacketWriter writer);
}
