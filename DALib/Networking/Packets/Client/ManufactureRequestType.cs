namespace DALib.Networking.Packets.Client;

/// <summary>
///     The subtype byte (third body byte) of the C->S 0x55 <see cref="ManufacturePacket" />: which
///     manufacture action is being driven. Selects the variant and the tail layout.
/// </summary>
public enum ManufactureRequestType : byte
{
    /// <summary>Request a recipe page by index (browse the manufacture window).</summary>
    RequestPage = 0,

    /// <summary>Craft the selected recipe, consuming an add-item from a slot.</summary>
    Make = 1
}
