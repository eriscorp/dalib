using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     Item / gold shape of <see cref="WorldObject" /> - 13 wire bytes. Used for
///     <see cref="WorldObject.Sprite" /> values in <c>[0x8000, 0xC000)</c> (subtract
///     <c>0x8000</c> and dispatch to item or gold based on the residue). Sprites
///     outside the creature range that are also outside <c>[0x8000, 0xC000)</c> parse
///     as this shape but render as nothing.
/// </summary>
public sealed record ItemWorldObject : WorldObject
{
    /// <summary>
    ///     Palette / dye color index. Its interpretation depends on the item / gold
    ///     subtype the sprite range encodes.
    /// </summary>
    public byte Color { get; set; }

    internal override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);
        writer.WriteUInt32(Id);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteByte(Direction);
        writer.WriteByte(Unknown);
    }
}
