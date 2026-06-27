using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Crypto;

/// <summary>
///     C->S dialog obfuscation for opcodes <c>0x39</c> (NpcMainMenu) and <c>0x3A</c>
///     (DialogUse). Applied to the packet body BEFORE the normal stream encryption layer
///     wraps the frame.
/// </summary>
/// <remarks>
///     The transform prepends a 6-byte header
///     <c>[rand][rand][lenHi][lenLo][crcHi][crcLo]</c> to the body, then XOR-masks the
///     length field and the body+CRC using values derived from the two random bytes. The
///     CRC is standard CRC-16-CCITT (polynomial 0x1021) over the unmasked body;
///     <see cref="CrcCcitt" /> provides the exact step function. Both <see cref="Apply(byte[])" />
///     and <see cref="Remove" /> are pure functions (given the same seeded RNG for Apply),
///     so a body can be round-tripped through <see cref="Apply(byte[])" /> and back.
/// </remarks>
public static class DialogObfuscation
{
    /// <summary>
    ///     True iff the given C->S opcode carries a dialog obfuscation header. Only
    ///     <c>0x39</c> and <c>0x3A</c> qualify.
    /// </summary>
    public static bool AppliesTo(byte opcode) => opcode == 0x39 || opcode == 0x3A;

    /// <summary>
    ///     Wrap a dialog body with the 6-byte obfuscation header, producing the buffer
    ///     the normal encryption layer then encrypts. Uses <see cref="Random.Shared" />
    ///     for the two header random bytes.
    /// </summary>
    /// <param name="body">
    ///     The dialog body to wrap - e.g. for 0x39,
    ///     <c>[byte objectType][uint32 BE objectId][uint16 BE pursuitId]</c>. Must NOT
    ///     include the opcode byte.
    /// </param>
    /// <returns>
    ///     A buffer of length <c>6 + body.Length</c>, ready to be placed after the opcode
    ///     byte in the payload that goes into <see cref="CryptoState.EncryptClientPacket(byte, byte[])" />.
    /// </returns>
    public static byte[] Apply(byte[] body) => Apply(body, Random.Shared);

    /// <summary>
    ///     Deterministic variant taking an explicit <see cref="Random" /> instance. Used
    ///     by tests to make output byte-for-byte reproducible.
    /// </summary>
    public static byte[] Apply(byte[] body, Random rng)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(rng);

        var output = new byte[6 + body.Length];
        var crc = (ushort)0;

        for (var i = 0; i < body.Length; i++)
        {
            output[6 + i] = body[i];
            crc = CrcCcitt.Step(crc, body[i]);
        }

        // Plaintext header. The length field is the number of bytes the XOR mask
        // covers (CRC hi+lo + body = body.Length + 2).
        var lengthField = body.Length + 2;
        output[0] = (byte)rng.Next(256);
        output[1] = (byte)rng.Next(256);
        output[2] = (byte)(lengthField >> 8);
        output[3] = (byte)(lengthField & 0xFF);
        output[4] = (byte)(crc >> 8);
        output[5] = (byte)(crc & 0xFF);

        // XOR mask derivation.
        var xPrime = (byte)(output[0] - 0x2D);
        var x = (byte)(output[1] ^ xPrime);
        var y = (byte)(x + 0x72);
        var z = (byte)(x + 0x28);

        output[2] ^= y;
        output[3] ^= (byte)((y + 1) & 0xFF);

        for (var i = 0; i < lengthField; i++)
            output[4 + i] ^= (byte)((z + i) & 0xFF);

        return output;
    }

    /// <summary>
    ///     Reverse <see cref="Apply(byte[])" />: unmask the buffer, validate the CRC, and return
    ///     the original body. Send paths do not call this; it is provided so tests can
    ///     round-trip a body through <see cref="Apply(byte[])" /> and back.
    /// </summary>
    /// <exception cref="InvalidDataException">
    ///     The buffer is too short, the embedded length is inconsistent with the buffer,
    ///     or the CRC doesn't match the unmasked body.
    /// </exception>
    public static byte[] Remove(byte[] obfuscated)
    {
        ArgumentNullException.ThrowIfNull(obfuscated);

        if (obfuscated.Length < 6)
            throw new InvalidDataException(
                $"Dialog packet must be at least 6 bytes; got {obfuscated.Length}.");

        var xPrime = (byte)(obfuscated[0] - 0x2D);
        var x = (byte)(obfuscated[1] ^ xPrime);
        var y = (byte)(x + 0x72);
        var z = (byte)(x + 0x28);

        var lengthHi = (byte)(obfuscated[2] ^ y);
        var lengthLo = (byte)(obfuscated[3] ^ ((y + 1) & 0xFF));
        var lengthField = (lengthHi << 8) | lengthLo;

        if (lengthField < 2 || 4 + lengthField > obfuscated.Length)
            throw new InvalidDataException(
                $"Dialog length field ({lengthField}) inconsistent with buffer length ({obfuscated.Length}).");

        var crcHi = (byte)(obfuscated[4] ^ z);
        var crcLo = (byte)(obfuscated[5] ^ ((z + 1) & 0xFF));
        var expectedCrc = (ushort)((crcHi << 8) | crcLo);

        var bodyLen = lengthField - 2;
        var body = new byte[bodyLen];
        var actualCrc = (ushort)0;

        for (var i = 0; i < bodyLen; i++)
        {
            var unmasked = (byte)(obfuscated[6 + i] ^ ((z + 2 + i) & 0xFF));
            body[i] = unmasked;
            actualCrc = CrcCcitt.Step(actualCrc, unmasked);
        }

        if (actualCrc != expectedCrc)
            throw new InvalidDataException(
                $"Dialog CRC mismatch: header carries 0x{expectedCrc:X4}, body hashes to 0x{actualCrc:X4}.");

        return body;
    }
}
