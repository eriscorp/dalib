using System;
using System.Security.Cryptography;
using System.Text;

namespace DALib.Cryptography;

/// <summary>
///     Provides cryptographic services for packets.
/// </summary>
[Obsolete(
    "Superseded by the DALib.Networking packet layer (DALib.Networking.Crypto.CryptoState and " +
    "DALib.Networking.Wire.PacketCodec). This type will be removed in a future major version.")]
public class PacketCryptoProvider
{
    private const int MAXIMUM_SEED = 9;
    private const int DEFAULT_SEED = 0;
    private const int SALT_LENGTH = 256;
    private const int KEYSTREAM_LENGTH = 9;
    private const string DEFAULT_KEYSTREAM = "UrkcnItnI";
    private const int KEYSTREAM2_TABLE_LENGTH = 1024;
    private const int BUFFER_LENGTH = 65532;
    private readonly byte[] _buffer;
    private readonly byte[] _keystream2;

    private readonly MD5 _md5;
    private byte[] _keystream1;
    private byte[] _keystream2Table;
    private uint _randState;
    private byte[] _salt = null!;

    private byte _seed;

    /// <summary>
    ///     Gets or sets the Keystream property, which represents the keystream as a string.
    /// </summary>
    /// <value>
    ///     The keystream as a string.
    /// </value>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the value is null.
    /// </exception>
    /// <exception cref="Exception">
    ///     Thrown if the value length is not equal to KEYSTREAM_LENGTH.
    /// </exception>
    public string Keystream
    {
        get => Encoding.ASCII.GetString(_keystream1);

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            var bytes = Encoding.ASCII.GetBytes(value);

            if (bytes.Length != KEYSTREAM_LENGTH)
                throw new Exception($"Keystream must be {KEYSTREAM_LENGTH} characters long");

            _keystream1 = bytes;
        }
    }

    /// <summary>
    ///     Gets or sets the seed value.
    /// </summary>
    /// <value>
    ///     The value of the seed.
    /// </value>
    /// <remarks>
    ///     Setting a new seed value will regenerate the salt using the new seed value.
    /// </remarks>
    public byte Seed
    {
        get => _seed;

        set
        {
            if (_seed != value)
            {
                _seed = value;
                GenerateSalt(value);
            }
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PacketCryptoProvider" /> class. Uses the default seed and keystream
    ///     values.
    /// </summary>
    public PacketCryptoProvider()
        : this(DEFAULT_SEED, DEFAULT_KEYSTREAM)
    {
        _keystream1[3] = 0xE5; // The default keystream is supposed to be UrkcnItnI,
        _keystream1[7] = 0xA3; // however Kru somehow managed to fuck that up. :-)
    }

    /// <summary>
    ///     Represents a packet crypto provider that encrypts and decrypts packets using a seed and a keystream.
    /// </summary>
    /// <remarks>
    ///     The PacketCryptoProvider uses a seed value and a keystream to generate a salt and performs packet encryption and
    ///     decryption operations.
    /// </remarks>
    public PacketCryptoProvider(byte seed, string keystream)
    {
        if (seed > MAXIMUM_SEED)
            throw new ArgumentOutOfRangeException(nameof(seed));

        ArgumentNullException.ThrowIfNull(keystream);

        var keystreamBytes = Encoding.ASCII.GetBytes(keystream);

        if (keystreamBytes.Length != KEYSTREAM_LENGTH)
            throw new ArgumentOutOfRangeException(nameof(keystream));

        _seed = seed;
        _keystream1 = keystreamBytes;
        _keystream2 = new byte[KEYSTREAM_LENGTH];
        _keystream2Table = new byte[KEYSTREAM2_TABLE_LENGTH];
        GenerateSalt(seed);

        _md5 = MD5.Create();
        _randState = 1;
        _buffer = new byte[BUFFER_LENGTH];
    }

    /// <summary>
    ///     Decrypts client data using the specified parameters.
    /// </summary>
    /// <param name="data">
    ///     The byte array containing the encrypted client data.
    /// </param>
    /// <param name="offset">
    ///     The starting index within the byte array.
    /// </param>
    /// <param name="count">
    ///     The number of bytes to be decrypted.
    /// </param>
    /// <param name="useKeystream2">
    ///     Specifies whether to use keystream 2 for decryption.
    /// </param>
    /// <returns>
    ///     The decrypted client data as a byte array.
    /// </returns>
    public byte[] DecryptClientData(
        byte[] data,
        int offset,
        int count,
        bool useKeystream2)
    {
        if (offset >= data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((offset + count) > data.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        _buffer[0] = data[0];

        Buffer.BlockCopy(
            data,
            offset + 2,
            _buffer,
            1,
            count - 9);

        var resultLength = offset + count - 3;

        var a = (ushort)((data[resultLength + 2] << 8) | data[resultLength]);
        var b = data[resultLength + 1];

        a ^= 0x7470;
        b ^= 0x23;

        resultLength -= 4; // hash bytes

        GenerateKeystream2(a, b);

        Transform(
            _buffer,
            1,
            count - 9,
            useKeystream2 ? _keystream2 : _keystream1,
            data[offset + 1]);

        if (useKeystream2)
            --resultLength; // opcode

        --resultLength; // trailing 0

        var result = new byte[resultLength];

        Buffer.BlockCopy(
            _buffer,
            0,
            result,
            0,
            resultLength);

        return result;
    }

    /// <summary>
    ///     Decrypts server data.
    /// </summary>
    /// <param name="data">
    ///     The data to decrypt.
    /// </param>
    /// <param name="offset">
    ///     The starting offset in the data.
    /// </param>
    /// <param name="count">
    ///     The number of bytes to decrypt.
    /// </param>
    /// <param name="useKeystream2">
    ///     Flag to indicate if keystream2 should be used for transformation.
    /// </param>
    /// <returns>
    ///     The decrypted server data as a byte array.
    /// </returns>
    public byte[] DecryptServerData(
        byte[] data,
        int offset,
        int count,
        bool useKeystream2)
    {
        if (offset >= data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((offset + count) > data.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        var a = (ushort)((data[count - 1] << 8) | data[count - 3]);
        var b = data[count - 2];

        a ^= 0x6474;
        b ^= 0x24;

        _buffer[0] = data[offset];

        Buffer.BlockCopy(
            data,
            offset + 2,
            _buffer,
            1,
            count - 5);

        GenerateKeystream2(a, b);

        Transform(
            _buffer,
            1,
            count - 5,
            useKeystream2 ? _keystream2 : _keystream1,
            data[offset + 1]);

        var result = new byte[count - 4];

        Buffer.BlockCopy(
            _buffer,
            0,
            result,
            0,
            count - 4);

        return result;
    }

    /// <summary>
    ///     Encrypts the provided client data using the specified parameters.
    /// </summary>
    /// <param name="data">
    ///     The data to encrypt.
    /// </param>
    /// <param name="offset">
    ///     The starting index in the data array from which to begin encryption.
    /// </param>
    /// <param name="count">
    ///     The number of bytes to encrypt.
    /// </param>
    /// <param name="sequence">
    ///     The sequence value to use during encryption.
    /// </param>
    /// <param name="useKeystream2">
    ///     Determines whether to use Keystream2 during encryption.
    /// </param>
    /// <returns>
    ///     The encrypted client data.
    /// </returns>
    public byte[] EncryptClientData(
        byte[] data,
        int offset,
        int count,
        byte sequence,
        bool useKeystream2)
    {
        if (offset >= data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((offset + count) > data.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        var resultLength = 0;

        _buffer[resultLength++] = data[offset];
        _buffer[resultLength++] = sequence;

        Buffer.BlockCopy(
            data,
            offset + 1,
            _buffer,
            2,
            count - 1);

        resultLength += count - 1;

        _buffer[resultLength++] = 0;

        if (useKeystream2)
            _buffer[resultLength++] = data[offset];

        var keystream2Seed = NextRandState(ref _randState);
        var a = (ushort)((ushort)keystream2Seed % 65277 + 256);
        var b = (byte)(((keystream2Seed & 0xFF0000) >> 16) % 155 + 100);

        GenerateKeystream2(a, b);

        Transform(
            _buffer,
            2,
            count - 1,
            useKeystream2 ? _keystream2 : _keystream1,
            sequence);

        var hash = _md5.ComputeHash(_buffer, 0, resultLength);
        _buffer[resultLength++] = hash[13];
        _buffer[resultLength++] = hash[3];
        _buffer[resultLength++] = hash[11];
        _buffer[resultLength++] = hash[7];

        a ^= 0x7470;
        b ^= 0x23;

        _buffer[resultLength++] = (byte)a;
        _buffer[resultLength++] = b;
        _buffer[resultLength++] = (byte)(a >> 8);

        var result = new byte[resultLength];

        Buffer.BlockCopy(
            _buffer,
            0,
            result,
            0,
            resultLength);

        return result;
    }

    /// <summary>
    ///     Encrypts server data using the specified parameters.
    /// </summary>
    /// <param name="data">
    ///     The data to be encrypted.
    /// </param>
    /// <param name="offset">
    ///     The starting offset within the data array.
    /// </param>
    /// <param name="count">
    ///     The number of bytes to be encrypted.
    /// </param>
    /// <param name="sequence">
    ///     The sequence number used for encryption.
    /// </param>
    /// <param name="useKeystream2">
    ///     Determines whether to use the second keystream for encryption.
    /// </param>
    /// <returns>
    ///     The encrypted server data as a byte array.
    /// </returns>
    public byte[] EncryptServerData(
        byte[] data,
        int offset,
        int count,
        byte sequence,
        bool useKeystream2)
    {
        if (offset >= data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((offset + count) > data.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        var resultLength = 0;

        _buffer[resultLength++] = data[offset];
        _buffer[resultLength++] = sequence;

        Buffer.BlockCopy(
            data,
            offset + 1,
            _buffer,
            2,
            count - 1);

        resultLength += count - 1;

        var keystream2Seed = NextRandState(ref _randState);
        var a = (ushort)((ushort)keystream2Seed % 65277 + 256);
        var b = (byte)(((keystream2Seed & 0xFF0000) >> 16) % 155 + 100);

        GenerateKeystream2(a, b);

        Transform(
            _buffer,
            2,
            count - 1,
            useKeystream2 ? _keystream2 : _keystream1,
            sequence);

        a ^= 0x6474;
        b ^= 0x24;

        _buffer[resultLength++] = (byte)a;
        _buffer[resultLength++] = b;
        _buffer[resultLength++] = (byte)(a >> 8);

        var result = new byte[resultLength];

        Buffer.BlockCopy(
            _buffer,
            0,
            result,
            0,
            resultLength);

        return result;
    }

    private void GenerateKeystream2(ushort a, byte b)
    {
        for (var i = 0; i < KEYSTREAM_LENGTH; ++i)
            _keystream2[i] = _keystream2Table[(i * (KEYSTREAM_LENGTH * i + b * b) + a) % KEYSTREAM2_TABLE_LENGTH];
    }

    /// <summary>
    ///     Generates a keystream table based on the given name.
    /// </summary>
    /// <param name="name">
    ///     The name to generate the keystream table from.
    /// </param>
    public void GenerateKeystream2Table(string name)
    {
        var table = GetMd5String(GetMd5String(name));

        for (var i = 0; i < 31; ++i)
            table += GetMd5String(table);

        _keystream2Table = Encoding.ASCII.GetBytes(table);
    }

    private void GenerateSalt(byte seed)
    {
        _salt = new byte[SALT_LENGTH];

        var saltByte = 0;

        for (var i = 0; i < SALT_LENGTH; ++i)
        {
            switch (seed)
            {
                case 0:
                    saltByte = i;

                    break;
                case 1:
                    saltByte = ((i % 2) != 0 ? -1 : 1) * ((i + 1) / 2) + 128;

                    break;
                case 2:
                    saltByte = 255 - i;

                    break;
                case 3:
                    saltByte = ((i % 2) != 0 ? -1 : 1) * ((255 - i) / 2) + 128;

                    break;
                case 4:
                    saltByte = i / 16 * (i / 16);

                    break;
                case 5:
                    saltByte = 2 * i % 256;

                    break;
                case 6:
                    saltByte = 255 - 2 * i % 256;

                    break;
                case 7:
                    if (i > 127)
                        saltByte = 2 * i - 256;
                    else
                        saltByte = 255 - 2 * i;

                    break;
                case 8:
                    if (i > 127)
                        saltByte = 511 - 2 * i;
                    else
                        saltByte = 2 * i;

                    break;
                case 9:
                    saltByte = 255 - (i - 128) / 8 * ((i - 128) / 8) % 256;

                    break;
            }

            saltByte |= (saltByte << 8) | ((saltByte | (saltByte << 8)) << 16);
            _salt[i] = (byte)saltByte;
        }
    }

    private string GetMd5String(string value)
    {
        var valueBytes = Encoding.ASCII.GetBytes(value);
        var hashBytes = _md5.ComputeHash(valueBytes);

        return BitConverter.ToString(hashBytes)
                           .Replace("-", string.Empty)
                           .ToLower();
    }

    private uint NextRandState(ref uint state)
    {
        state = state * 0x343FD + 0x269EC3;

        return (state >> 0x10) & 0x7FFF;
    }

    private void Transform(
        byte[] buffer,
        int offset,
        int count,
        byte[] keystream,
        byte sequence)
    {
        if (offset >= buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((offset + count) > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        for (var i = 0; i < count; ++i)
        {
            buffer[i + offset] ^= _salt[sequence];
            buffer[i + offset] ^= keystream[i % KEYSTREAM_LENGTH];

            var saltIndex = i / KEYSTREAM_LENGTH % SALT_LENGTH;

            if (saltIndex != sequence)
                buffer[i + offset] ^= _salt[saltIndex];
        }
    }
}