using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace DALib.Networking.Crypto;

/// <summary>
///     Per-connection encryption state for the DOOMVAS v1 protocol.
/// </summary>
/// <remarks>
///     <para>
///         Tracks the lobby-handshake seed and key, the character-name-derived 1024-byte
///         key table, and the per-direction ordinal counters. Provides encrypt and decrypt
///         for both directions.
///     </para>
///     <para>
///         The salt table is generated from the protocol's 10 closed-form seed formulas
///         (see <see cref="GenerateSalt" />).
///     </para>
/// </remarks>
public sealed class CryptoState
{
    /// <summary>
    ///     Encryption seed (0-8), assigned during lobby phase via the 0x00 S->C response.
    ///     Indexes into the salt table.
    /// </summary>
    public byte EncryptionSeed { get; set; }

    /// <summary>
    ///     9-byte encryption key, assigned during lobby phase via the 0x00 S->C response.
    ///     Used for <see cref="EncryptMethod.Normal" />-encrypted packets.
    /// </summary>
    public byte[] EncryptionKey { get; set; } = [];

    /// <summary>
    ///     1024-byte key table, generated from character name via iterated MD5 hashing.
    ///     Used to derive per-packet keys for <see cref="EncryptMethod.MD5Key" />-encrypted
    ///     packets.
    /// </summary>
    public byte[] EncryptionKeyTable { get; private set; } = new byte[1024];

    /// <summary>
    ///     Ordinal counter for S->C encrypted packets. Incremented after each decrypted /
    ///     encrypted packet.
    /// </summary>
    public byte ServerOrdinal { get; set; }

    /// <summary>
    ///     Ordinal counter for C->S encrypted packets. Incremented after each decrypted /
    ///     encrypted packet.
    /// </summary>
    public byte ClientOrdinal { get; set; }

    /// <summary>
    ///     True if the lobby handshake has supplied seed and key.
    /// </summary>
    public bool IsInitialized => EncryptionKey.Length > 0;

    /// <summary>
    ///     The salt table row for the current <see cref="EncryptionSeed" />.
    /// </summary>
    public ImmutableArray<byte> GetSalt() => SaltTable[EncryptionSeed];

    // ----------------------------------------------------------------
    // Opcode -> EncryptMethod lookup
    // ----------------------------------------------------------------

    /// <summary>
    ///     Determine the encryption method for a C->S (client) packet by opcode.
    /// </summary>
    public static EncryptMethod GetClientEncryptMethod(byte opcode) => opcode switch
    {
        0x00 or 0x10 or 0x48 => EncryptMethod.None,
        0x02 or 0x03 or 0x04 or 0x0B or 0x26 or 0x2D or 0x3A or 0x42
            or 0x43 or 0x4B or 0x57 or 0x62 or 0x68 or 0x71 or 0x73 or 0x7B
            => EncryptMethod.Normal,
        _ => EncryptMethod.MD5Key
    };

    /// <summary>
    ///     Determine the encryption method for a S->C (server) packet by opcode.
    /// </summary>
    public static EncryptMethod GetServerEncryptMethod(byte opcode) => opcode switch
    {
        0x00 or 0x03 or 0x40 or 0x7E => EncryptMethod.None,
        0x01 or 0x02 or 0x0A or 0x56 or 0x60 or 0x62 or 0x66 or 0x6F
            => EncryptMethod.Normal,
        _ => EncryptMethod.MD5Key
    };

    // ----------------------------------------------------------------
    // Key generation
    // ----------------------------------------------------------------

    /// <summary>
    ///     Build the 1024-byte key table from a character name. MD5-hash the name twice,
    ///     then concatenate 31 more iterated hashes of the running string. Result is
    ///     1024 lowercase hex characters interpreted as ASCII bytes.
    /// </summary>
    public void GenerateKeyTable(string characterName)
    {
        var table = Md5HashString(characterName);
        table = Md5HashString(table);

        for (var i = 0; i < 31; i++)
            table += Md5HashString(table);

        EncryptionKeyTable = Encoding.ASCII.GetBytes(table);
    }

    /// <summary>
    ///     Generate a 9-byte key for <see cref="EncryptMethod.MD5Key" />-encrypted packets
    ///     from per-packet bRand / sRand footer values.
    /// </summary>
    public byte[] GenerateKey(ushort bRand, byte sRand)
    {
        var key = new byte[9];

        for (var i = 0; i < 9; i++)
            key[i] = EncryptionKeyTable[(i * (9 * i + sRand * sRand) + bRand) % 1024];

        return key;
    }

    // ----------------------------------------------------------------
    // State extraction from lobby packets
    // ----------------------------------------------------------------

    /// <summary>
    ///     Extract encryption seed and key from a 0x00 S->C lobby response.
    /// </summary>
    /// <remarks>
    ///     Payload layout (including opcode at [0]):
    ///     <c>[0] = 0x00 (opcode), [1] = 0x00 (unknown), [2..5] = serverTableCrc (uint32),
    ///     [6] = seed, [7] = keyLength, [8..8+keyLength] = key</c>.
    /// </remarks>
    public void UpdateFromLobbyResponse(byte[] payload)
    {
        if (payload.Length < 9)
            return;

        EncryptionSeed = payload[6];
        var keyLength = payload[7];

        if (payload.Length < 8 + keyLength)
            return;

        EncryptionKey = new byte[keyLength];
        Array.Copy(payload, 8, EncryptionKey, 0, keyLength);
    }

    // ----------------------------------------------------------------
    // Decrypt C->S
    // ----------------------------------------------------------------

    /// <summary>
    ///     Decrypt a C->S packet payload in place.
    /// </summary>
    /// <remarks>
    ///     <paramref name="data" /> is the payload data AFTER the ordinal byte (i.e.,
    ///     wire-frame bytes <c>[5..end]</c> for encrypted packets). Returns the decrypted
    ///     data without the 7-byte footer, or the original data if the opcode is
    ///     <see cref="EncryptMethod.None" />. Increments <see cref="ClientOrdinal" /> after
    ///     successful decryption (mirrors <see cref="DecryptServer" />'s behavior).
    /// </remarks>
    public byte[] DecryptClient(byte opcode, byte ordinal, byte[] data)
    {
        var method = GetClientEncryptMethod(opcode);

        if (method == EncryptMethod.None)
            return data;

        // Real footer is 7 bytes: 4 MD5 hash + 3 bRand/sRand.
        var length = data.Length - 7;

        if (length < 0)
            return data;

        var bRandOffset = data.Length - 3;
        var bRand = (ushort)(((data[bRandOffset + 2] << 8) | data[bRandOffset]) ^ 0x7470);
        var sRand = (byte)(data[bRandOffset + 1] ^ 0x23);

        var key = method == EncryptMethod.Normal ? EncryptionKey : GenerateKey(bRand, sRand);
        var salt = SaltTable[EncryptionSeed];

        for (var i = 0; i < length; i++)
        {
            data[i] ^= key[i % key.Length];
            data[i] ^= salt[i / key.Length % salt.Length];

            if (i / key.Length % salt.Length != ordinal)
                data[i] ^= salt[ordinal];
        }

        ClientOrdinal++;

        var result = new byte[length];
        Array.Copy(data, 0, result, 0, length);

        return result;
    }

    // ----------------------------------------------------------------
    // Decrypt S->C
    // ----------------------------------------------------------------

    /// <summary>
    ///     Decrypt an S->C packet payload in place.
    /// </summary>
    /// <remarks>
    ///     <paramref name="data" /> is the payload data AFTER the ordinal byte. Returns the
    ///     decrypted data without the 3-byte footer (S->C has no MD5 hash in the footer).
    ///     Increments <see cref="ServerOrdinal" /> after successful decryption.
    /// </remarks>
    public byte[] DecryptServer(byte opcode, byte ordinal, byte[] data)
    {
        var method = GetServerEncryptMethod(opcode);

        if (method == EncryptMethod.None)
            return data;

        var length = data.Length - 3;

        if (length < 0)
            return data;

        var bRand = (ushort)(((data[length + 2] << 8) | data[length]) ^ 0x6474);
        var sRand = (byte)(data[length + 1] ^ 0x24);

        var key = method == EncryptMethod.Normal ? EncryptionKey : GenerateKey(bRand, sRand);
        var salt = SaltTable[EncryptionSeed];

        for (var i = 0; i < length; i++)
        {
            data[i] ^= key[i % key.Length];
            data[i] ^= salt[i / key.Length % salt.Length];

            if (i / key.Length % salt.Length != ordinal)
                data[i] ^= salt[ordinal];
        }

        ServerOrdinal++;

        var result = new byte[length];
        Array.Copy(data, 0, result, 0, length);

        return result;
    }

    // ----------------------------------------------------------------
    // Encrypt C->S
    // ----------------------------------------------------------------

    /// <summary>
    ///     Encrypt a plaintext C->S payload using the auto-allocated <see cref="ClientOrdinal" />.
    ///     Advances the counter on success.
    /// </summary>
    /// <remarks>
    ///     Input: <c>[opcode][data...]</c> (plaintext). Output:
    ///     <c>[opcode][ordinal][encrypted data...][7-byte footer]</c> - ready to be wrapped
    ///     in the outer <c>[0xAA][u16-BE len][...]</c> frame.
    /// </remarks>
    public byte[] EncryptClientPacket(byte opcode, byte[] plainPayload)
    {
        var result = EncryptClientPacket(opcode, plainPayload, ClientOrdinal);

        if (GetClientEncryptMethod(opcode) != EncryptMethod.None)
            ClientOrdinal++;

        return result;
    }

    /// <summary>
    ///     Encrypt a plaintext C->S payload using an explicit ordinal. Does not touch
    ///     <see cref="ClientOrdinal" /> - used for testing and protocol probing.
    /// </summary>
    public byte[] EncryptClientPacket(byte opcode, byte[] plainPayload, byte ordinal)
    {
        var method = GetClientEncryptMethod(opcode);

        if (method == EncryptMethod.None)
            return plainPayload;

        // Inner-plaintext padding before encryption.
        //   Always: trailing 0x00.
        //   MD5Key only: trailing opcode copy.
        var payloadLen = plainPayload.Length - 1;
        var extraLen = method == EncryptMethod.MD5Key ? 2 : 1;
        var data = new byte[payloadLen + extraLen];

        if (payloadLen > 0)
            Buffer.BlockCopy(plainPayload, 1, data, 0, payloadLen);

        data[payloadLen] = 0x00;

        if (method == EncryptMethod.MD5Key)
            data[payloadLen + 1] = opcode;

        var bRand = (ushort)(Random.Shared.Next(65277) + 256);
        var sRand = (byte)(Random.Shared.Next(155) + 100);

        var key = method == EncryptMethod.Normal ? EncryptionKey : GenerateKey(bRand, sRand);
        var salt = SaltTable[EncryptionSeed];

        for (var i = 0; i < data.Length; i++)
        {
            data[i] ^= key[i % key.Length];
            data[i] ^= salt[i / key.Length % salt.Length];

            if (i / key.Length % salt.Length != ordinal)
                data[i] ^= salt[ordinal];
        }

        // Output: [opcode][ordinal][encrypted data][7-byte footer]
        var result = new byte[2 + data.Length + 7];
        result[0] = opcode;
        result[1] = ordinal;
        Buffer.BlockCopy(data, 0, result, 2, data.Length);

        // 4-byte MD5 hash footer.
        var hashInput = new byte[2 + data.Length];
        Buffer.BlockCopy(result, 0, hashInput, 0, hashInput.Length);
        var md5 = MD5.HashData(hashInput);

        var footerStart = 2 + data.Length;
        result[footerStart]     = md5[13];
        result[footerStart + 1] = md5[3];
        result[footerStart + 2] = md5[11];
        result[footerStart + 3] = md5[7];

        // 3-byte bRand/sRand footer.
        result[footerStart + 4] = (byte)((bRand & 0xFF) ^ 0x70);
        result[footerStart + 5] = (byte)(sRand ^ 0x23);
        result[footerStart + 6] = (byte)(((bRand >> 8) & 0xFF) ^ 0x74);

        return result;
    }

    // ----------------------------------------------------------------
    // Encrypt S->C
    // ----------------------------------------------------------------

    /// <summary>
    ///     Encrypt a plaintext S->C payload using the auto-allocated <see cref="ServerOrdinal" />.
    ///     Advances the counter on success.
    /// </summary>
    /// <remarks>
    ///     Input: <c>[opcode][data...]</c> (plaintext). Output:
    ///     <c>[opcode][ordinal][encrypted data...][3-byte footer]</c>. S->C does not carry
    ///     an MD5 hash in the footer and does not have inner-plaintext padding.
    /// </remarks>
    public byte[] EncryptServerPacket(byte opcode, byte[] plainPayload)
    {
        var result = EncryptServerPacket(opcode, plainPayload, ServerOrdinal);

        if (GetServerEncryptMethod(opcode) != EncryptMethod.None)
            ServerOrdinal++;

        return result;
    }

    /// <summary>
    ///     Encrypt a plaintext S->C payload using an explicit ordinal. Does not touch
    ///     <see cref="ServerOrdinal" /> - used for testing and protocol probing.
    /// </summary>
    public byte[] EncryptServerPacket(byte opcode, byte[] plainPayload, byte ordinal)
    {
        var method = GetServerEncryptMethod(opcode);

        if (method == EncryptMethod.None)
            return plainPayload;

        var dataLen = plainPayload.Length - 1;
        var data = new byte[dataLen];

        if (dataLen > 0)
            Buffer.BlockCopy(plainPayload, 1, data, 0, dataLen);

        var bRand = (ushort)(Random.Shared.Next(65277) + 256);
        var sRand = (byte)(Random.Shared.Next(155) + 100);

        var key = method == EncryptMethod.Normal ? EncryptionKey : GenerateKey(bRand, sRand);
        var salt = SaltTable[EncryptionSeed];

        for (var i = 0; i < data.Length; i++)
        {
            data[i] ^= key[i % key.Length];
            data[i] ^= salt[i / key.Length % salt.Length];

            if (i / key.Length % salt.Length != ordinal)
                data[i] ^= salt[ordinal];
        }

        // Output: [opcode][ordinal][encrypted data][3-byte footer]
        var result = new byte[2 + data.Length + 3];
        result[0] = opcode;
        result[1] = ordinal;
        Buffer.BlockCopy(data, 0, result, 2, data.Length);

        var footerStart = 2 + data.Length;
        result[footerStart]     = (byte)((bRand & 0xFF) ^ 0x74);
        result[footerStart + 1] = (byte)(sRand ^ 0x24);
        result[footerStart + 2] = (byte)(((bRand >> 8) & 0xFF) ^ 0x64);

        return result;
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static string Md5HashString(string value)
    {
        var buffer = Encoding.ASCII.GetBytes(value);
        var hash = MD5.HashData(buffer);

        return Convert.ToHexStringLower(hash);
    }

    // ----------------------------------------------------------------
    // Salt table - formulaic, 10 seeds x 256 bytes each.
    // ----------------------------------------------------------------

    private static readonly ImmutableArray<byte>[] SaltTable = BuildSaltTable();

    private static ImmutableArray<byte>[] BuildSaltTable()
    {
        var table = new ImmutableArray<byte>[10];

        for (byte seed = 0; seed < 10; seed++)
            table[seed] = ImmutableCollectionsMarshal.AsImmutableArray(GenerateSalt(seed));

        return table;
    }

    /// <summary>
    ///     Compute the 256-byte salt row for the given seed. Each seed has a closed-form
    ///     definition; values that fall outside <c>[0, 255]</c> wrap via the implicit
    ///     <c>(byte)</c> cast.
    /// </summary>
    private static byte[] GenerateSalt(byte seed)
    {
        var salt = new byte[256];

        for (var i = 0; i < 256; i++)
        {
            var value = seed switch
            {
                0 => i,
                1 => ((i % 2) != 0 ? -1 : 1) * ((i + 1) / 2) + 128,
                2 => 255 - i,
                3 => ((i % 2) != 0 ? -1 : 1) * ((255 - i) / 2) + 128,
                4 => i / 16 * (i / 16),
                5 => 2 * i % 256,
                6 => 255 - 2 * i % 256,
                7 => i > 127 ? 2 * i - 256 : 255 - 2 * i,
                8 => i > 127 ? 511 - 2 * i : 2 * i,
                9 => 255 - (i - 128) / 8 * ((i - 128) / 8) % 256,
                _ => throw new ArgumentOutOfRangeException(nameof(seed),
                    $"Salt seed must be 0-9, was {seed}.")
            };

            salt[i] = (byte)value;
        }

        return salt;
    }
}
