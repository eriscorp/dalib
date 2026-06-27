namespace DALib.Networking.Packets.Server;

/// <summary>
///     The subtype byte (third body byte) of S->C 0x50 <see cref="ManufactureResponsePacket" /> - which
///     manufacture-window update the server is sending. Selects the variant and the tail layout. Mirrors
///     the C->S <see cref="DALib.Networking.Packets.Client.ManufactureRequestType" />.
/// </summary>
public enum ManufactureResponseType : byte
{
    /// <summary>Open the manufacture window, announcing the recipe count.</summary>
    Open = 0,

    /// <summary>Display one recipe page - sprite, name, description, ingredients.</summary>
    Page = 1
}
