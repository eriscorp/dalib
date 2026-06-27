namespace DALib.Networking.Crypto;

/// <summary>
///     Encryption method assigned to an opcode by the DOOMVAS v1 protocol.
/// </summary>
/// <remarks>
///     The method is a property of the <em>opcode</em>, not the packet - see the static
///     <c>GetClientEncryptMethod</c> and <c>GetServerEncryptMethod</c> tables on
///     <see cref="CryptoState" /> for the per-direction opcode -&gt; method maps.
/// </remarks>
public enum EncryptMethod
{
    /// <summary>
    ///     No encryption. Used by the handful of packets that fire before crypto state
    ///     exists (e.g., handshake-era packets like 0x00 CryptoKey, 0x03 Redirect).
    /// </summary>
    None,

    /// <summary>
    ///     Stream encryption using the session-wide 9-byte key handed over in the lobby
    ///     0x00 response. Used by most lobby-era and login-era traffic.
    /// </summary>
    Normal,

    /// <summary>
    ///     Stream encryption using a per-packet key derived from the character-name-derived
    ///     1024-byte key table plus per-packet bRand/sRand values carried in the footer.
    ///     Used by most in-world traffic.
    /// </summary>
    MD5Key
}
