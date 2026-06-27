namespace DALib.Networking.Wire;

/// <summary>
///     Wire-frame constants shared by the client and server codecs.
/// </summary>
internal static class WireFrame
{
    /// <summary>
    ///     Outer frame marker. Every DOOMVAS v1 packet starts with this byte.
    /// </summary>
    internal const byte Marker = 0xAA;

    /// <summary>
    ///     Size of the outer header: <c>[marker][u16-BE body-length]</c>.
    /// </summary>
    internal const int HeaderLength = 3;

    /// <summary>
    ///     Size of the opcode field at the start of the wire body.
    /// </summary>
    internal const int OpcodeLength = 1;

    /// <summary>
    ///     Value of the trailing byte appended to C->S frames. S->C frames have no equivalent.
    /// </summary>
    internal const byte TrailingNull = 0x00;

    /// <summary>
    ///     Size of the C->S trailing byte field.
    /// </summary>
    internal const int TrailingNullLength = 1;
}
