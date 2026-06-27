using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x55 (C->S) - a step in a manufacture (crafting) dialog. The body opens with a shared prefix
///     <c>[u8 ManufactureType][u8 Slot][u8 subtype]</c>; the subtype byte (a
///     <see cref="ManufactureRequestType" />) selects the form and any tail. The concrete forms are
///     the sealed records deriving from this base (<see cref="RequestManufacturePagePacket" />,
///     <see cref="MakeManufacturePacket" />). The <c>ManufactureType</c>/<c>Slot</c> pair is a
///     session token chosen by the server when it opens the dialog with the S->C 0x50 and echoed
///     back on every C->S 0x55 so the action can be validated against the open window.
/// </summary>
[ClientOpcode(ClientOpcode.Manufacture)]
public abstract record ManufacturePacket : ClientPacket
{
    /// <summary>The subtype byte that selects this variant's form.</summary>
    public abstract ManufactureRequestType RequestType { get; }

    /// <summary>The manufacture window's type token, echoed from the S->C 0x50 that opened the dialog.</summary>
    public required byte ManufactureType { get; init; }

    /// <summary>The window's slot token, echoed from the S->C 0x50 that opened the dialog.</summary>
    public required byte Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Manufacture;

    /// <summary>Writes the shared <c>[u8 ManufactureType][u8 Slot][u8 subtype]</c> prefix. Variants
    ///     call this, then append their tail.</summary>
    protected void WritePrefix(IPacketWriter writer)
    {
        writer.WriteByte(ManufactureType);
        writer.WriteByte(Slot);
        writer.WriteByte((byte)RequestType);
    }

    /// <summary>
    ///     Parses a 0x55 body, dispatching on the subtype byte (the third body byte) to the matching
    ///     variant. This is the standalone entry and what <see cref="ClientOpcodeAttribute" />
    ///     dispatch binds.
    /// </summary>
    public static ManufacturePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var manufactureType = reader.ReadByte();
        var slot = reader.ReadByte();
        var subtype = (ManufactureRequestType)reader.ReadByte();

        return subtype switch
        {
            ManufactureRequestType.RequestPage => new RequestManufacturePagePacket
                { ManufactureType = manufactureType, Slot = slot, PageIndex = reader.ReadByte() },
            ManufactureRequestType.Make => new MakeManufacturePacket
            {
                ManufactureType = manufactureType,
                Slot = slot,
                RecipeName = reader.ReadString8(),
                AddSlotIndex = reader.ReadByte()
            },
            _ => throw new InvalidDataException(
                $"0x55 Manufacture: unknown request type 0x{(byte)subtype:X2}.")
        };
    }
}

/// <summary>
///     0x55 subtype 0 - request a recipe page by index (browse the window). Tail
///     <c>[u8 PageIndex]</c>.
/// </summary>
public sealed record RequestManufacturePagePacket : ManufacturePacket
{
    /// <summary>The recipe page to display.</summary>
    public required byte PageIndex { get; init; }

    /// <inheritdoc />
    public override ManufactureRequestType RequestType => ManufactureRequestType.RequestPage;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(PageIndex);
    }
}

/// <summary>
///     0x55 subtype 1 - craft the named recipe, consuming an add-item from a slot. Tail
///     <c>[string8 RecipeName][u8 AddSlotIndex]</c>. The server validates
///     <see cref="RecipeName" /> against the currently selected recipe.
/// </summary>
public sealed record MakeManufacturePacket : ManufacturePacket
{
    /// <summary>The recipe to craft (validated against the selected page server-side).</summary>
    public required string RecipeName { get; init; }

    /// <summary>The inventory slot of the add-item the recipe consumes.</summary>
    public required byte AddSlotIndex { get; init; }

    /// <inheritdoc />
    public override ManufactureRequestType RequestType => ManufactureRequestType.Make;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteString8(RecipeName);
        writer.WriteByte(AddSlotIndex);
    }
}
