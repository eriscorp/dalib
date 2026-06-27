using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x50 (S->C) - the server opens or pages the manufacture (crafting) window. The body opens with a
///     shared prefix <c>[u8 ManufactureType][u8 Slot][u8 subtype]</c>; the subtype byte (a
///     <see cref="ManufactureResponseType" />) selects the form and any tail. The concrete forms are the
///     sealed records deriving from this base (<see cref="OpenManufacturePacket" />,
///     <see cref="ManufacturePagePacket" />). The C->S answer is 0x55
///     (<see cref="DALib.Networking.Packets.Client.ManufacturePacket" />).
/// </summary>
/// <remarks>
///     The <see cref="ManufactureType" />/<see cref="Slot" /> pair is a session token: the server picks
///     them when it opens the dialog, and they are echoed back on every C->S 0x55 to identify the open
///     window.
/// </remarks>
[ServerOpcode(ServerOpcode.Manufacture)]
public abstract record ManufactureResponsePacket : ServerPacket
{
    /// <summary>The subtype byte that selects this variant's form.</summary>
    public abstract ManufactureResponseType ResponseType { get; }

    /// <summary>The manufacture window's type token; echoed back on every C->S 0x55.</summary>
    public required byte ManufactureType { get; init; }

    /// <summary>The window's slot token; echoed back on every C->S 0x55.</summary>
    public required byte Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Manufacture;

    /// <summary>Writes the shared <c>[u8 ManufactureType][u8 Slot][u8 subtype]</c> prefix. Variants
    ///     call this, then append their tail.</summary>
    protected void WritePrefix(IPacketWriter writer)
    {
        writer.WriteByte(ManufactureType);
        writer.WriteByte(Slot);
        writer.WriteByte((byte)ResponseType);
    }

    /// <summary>
    ///     Parses a 0x50 body, dispatching on the subtype byte (the third body byte) to the matching
    ///     variant. This is what <see cref="ServerOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static ManufactureResponsePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var manufactureType = reader.ReadByte();
        var slot = reader.ReadByte();
        var subtype = (ManufactureResponseType)reader.ReadByte();

        return subtype switch
        {
            ManufactureResponseType.Open => new OpenManufacturePacket
                { ManufactureType = manufactureType, Slot = slot, RecipeCount = reader.ReadByte() },
            ManufactureResponseType.Page => new ManufacturePagePacket
            {
                ManufactureType = manufactureType,
                Slot = slot,
                PageIndex = reader.ReadByte(),
                Sprite = reader.ReadUInt16(),
                RecipeName = reader.ReadString8(),
                Description = reader.ReadString16(),
                Ingredients = reader.ReadString16(),
                HasAddItem = reader.ReadBoolean()
            },
            _ => throw new InvalidDataException(
                $"0x50 Manufacture: unknown response type 0x{(byte)subtype:X2}.")
        };
    }
}

/// <summary>
///     0x50 subtype 0 - open the manufacture window, announcing how many recipe pages it holds. Tail
///     <c>[u8 RecipeCount]</c>.
/// </summary>
public sealed record OpenManufacturePacket : ManufactureResponsePacket
{
    /// <summary>The number of recipe pages available in the window.</summary>
    public required byte RecipeCount { get; init; }

    /// <inheritdoc />
    public override ManufactureResponseType ResponseType => ManufactureResponseType.Open;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(RecipeCount);
    }
}

/// <summary>
///     0x50 subtype 1 - display one recipe page. Tail
///     <c>[u8 PageIndex][u16 BE Sprite][string8 RecipeName][string16 BE Description][string16 BE Ingredients][u8 HasAddItem]</c>.
/// </summary>
public sealed record ManufacturePagePacket : ManufactureResponsePacket
{
    /// <summary>The index of the recipe page being shown.</summary>
    public required byte PageIndex { get; init; }

    /// <summary>
    ///     The recipe's display sprite, raw on-wire with the item high bit set (<c>Tile + 0x8000</c>).
    ///     Round-tripped verbatim; clear the high bit to recover the tile id.
    /// </summary>
    public required ushort Sprite { get; init; }

    /// <summary>The recipe's name (<c>string8</c>).</summary>
    public required string RecipeName { get; init; }

    /// <summary>The recipe's description text (<c>string16</c>).</summary>
    public required string Description { get; init; }

    /// <summary>The recipe's ingredient list, server-formatted with availability highlighting (<c>string16</c>).</summary>
    public required string Ingredients { get; init; }

    /// <summary>Whether the recipe consumes an add-item (an extra inventory item required by the recipe).</summary>
    public required bool HasAddItem { get; init; }

    /// <inheritdoc />
    public override ManufactureResponseType ResponseType => ManufactureResponseType.Page;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(PageIndex);
        writer.WriteUInt16(Sprite);
        writer.WriteString8(RecipeName);
        writer.WriteString16(Description);
        writer.WriteString16(Ingredients);
        writer.WriteBoolean(HasAddItem);
    }
}
