namespace DALib.Networking.Packets.Server;

/// <summary>
///     Equipment-pane slot numbering shared by S->C 0x37 <see cref="AddEquipmentPacket" /> and 0x38
///     <see cref="RemoveEquipmentPacket" />. Slots 1-18 are valid; 0 and values >= 19 are ignored.
/// </summary>
public enum EquipmentSlot : byte
{
    /// <summary>0 - not a slot; ignored.</summary>
    None = 0,

    /// <summary>1 - weapon.</summary>
    Weapon = 1,

    /// <summary>2 - body armor.</summary>
    Armor = 2,

    /// <summary>3 - shield.</summary>
    Shield = 3,

    /// <summary>4 - helmet.</summary>
    Helmet = 4,

    /// <summary>5 - earrings.</summary>
    Earrings = 5,

    /// <summary>6 - necklace.</summary>
    Necklace = 6,

    /// <summary>7 - left ring.</summary>
    LeftRing = 7,

    /// <summary>8 - right ring.</summary>
    RightRing = 8,

    /// <summary>9 - left gauntlet.</summary>
    LeftGauntlet = 9,

    /// <summary>10 - right gauntlet.</summary>
    RightGauntlet = 10,

    /// <summary>11 - belt.</summary>
    Belt = 11,

    /// <summary>12 - greaves.</summary>
    Greaves = 12,

    /// <summary>13 - boots.</summary>
    Boots = 13,

    /// <summary>14 - first accessory.</summary>
    Accessory1 = 14,

    /// <summary>15 - overcoat (fashion armor drawn over the body armor).</summary>
    Overcoat = 15,

    /// <summary>16 - over-helm (fashion hat drawn instead of the helmet).</summary>
    OverHelm = 16,

    /// <summary>17 - second accessory.</summary>
    Accessory2 = 17,

    /// <summary>18 - third accessory.</summary>
    Accessory3 = 18
}
