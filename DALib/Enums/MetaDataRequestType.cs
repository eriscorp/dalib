namespace DALib.Enums;

/// <summary>
///     Selects what a C->S metafile request asks for. Byte values match the retail
///     wire encoding.
/// </summary>
public enum MetaDataRequestType : byte
{
    /// <summary>Request a single named metafile.</summary>
    DataByName = 0,

    /// <summary>Request the checksum table for all metafiles.</summary>
    AllCheckSums = 1
}
