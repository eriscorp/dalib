namespace DALib.Networking.Packets.Server;

/// <summary>
///     The leading body byte of an S->C 0x6F <see cref="MetafilePacket" /> that selects its form.
/// </summary>
public enum MetafileType : byte
{
    /// <summary>0 - one named metafile's content: <c>[string8 Name][u32 Checksum][u16 DataLen][bytes Data]</c>.
    ///     Answers a by-name request.</summary>
    DataByName = 0,

    /// <summary>1 - the checksum manifest for every metafile: <c>[u16 Count]{[string8 Name][u32 Checksum]}</c>.
    ///     Reconciled against the local cache; differing entries are re-requested.</summary>
    AllCheckSums = 1
}
