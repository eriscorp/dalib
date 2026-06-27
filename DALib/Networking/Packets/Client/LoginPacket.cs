using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x03 (C->S) - authenticate to the login server with username and password, followed by an
///     XOR-masked integrity trailer.
/// </summary>
/// <remarks>
///     <para>
///         Wire body:
///         <c>[string8 name][string8 password]</c> followed by a fixed 15-byte trailer:
///         <c>[byte rand1][byte xorKey_wire][u32-BE serverHash_xored][u16-BE clientHash_xored]
///         [u32-BE randData_xored][u16-BE crc_xored][0x01 marker]</c>.
///     </para>
///     <para>
///         Every multi-byte trailer field is masked with a per-byte XOR stream rooted at a
///         per-packet internal xor key. <c>xorKey_wire</c> on the wire is the internal key
///         obfuscated by <c>rand1</c>: <c>xorKey_wire = ((rand1 + 0x3B) &amp; 0xFF) ^ xorKey_internal</c>.
///         The internal key never appears on the wire; it is recovered from <c>rand1</c> and
///         <c>xorKey_wire</c>.
///     </para>
///     <para>
///         Reversed-mask detail: the mask sequence is applied to the field's little-endian memory
///         bytes, then re-serialized as big-endian on the wire. The net effect is that on the wire,
///         the high byte of each field gets the highest mask offset and the low byte gets the lowest.
///         For a u32 with mask base <c>0x8A</c>: <c>wire[0] ^= xor+0x8D</c>, <c>wire[1] ^= xor+0x8C</c>,
///         <c>wire[2] ^= xor+0x8B</c>, <c>wire[3] ^= xor+0x8A</c>.
///     </para>
///     <para>
///         The four trailer values are nullable init-only fields: set one explicitly to pin it to a
///         specific value (testing, replay), or leave it null to let the library supply a default.
///         The CRC is always computed from the assembled trailer bytes. <see cref="Rand1" /> is also
///         overrideable for deterministic wire-pin tests; leave it null otherwise.
///     </para>
/// </remarks>
[ClientOpcode(ClientOpcode.Login)]
public sealed record LoginPacket : ClientPacket
{
    /// <summary>Default value used for <see cref="ServerHash" /> when none is supplied.</summary>
    public const uint DefaultServerHash = 0xFF00FF00;

    private const byte FixedTrailerMarker = 0x01;
    private const byte Rand1Offset = 0x3B;
    private const byte ServerHashMaskBase = 0x8A;
    private const byte ClientHashMaskBase = 0x5E;
    private const byte RandDataMaskBase = 0x73;
    private const byte CrcMaskBase = 0xA5;

    /// <summary>Account / character name.</summary>
    public required string Name { get; init; }

    /// <summary>Plaintext password (the wire format is plaintext; the server hashes server-side).</summary>
    public required string Password { get; init; }

    /// <summary>
    ///     The per-packet random salt byte. Null -> generated per encode. Override only for
    ///     deterministic testing.
    /// </summary>
    public byte? Rand1 { get; init; }

    /// <summary>
    ///     The per-packet internal XOR key. Null -> generated per encode. Override only for
    ///     deterministic testing.
    /// </summary>
    public byte? XorKey { get; init; }

    /// <summary>Integrity-trailer serverHash value. Null -> <see cref="DefaultServerHash" />.</summary>
    public uint? ServerHash { get; init; }

    /// <summary>
    ///     Integrity-trailer clientHash value. Null -> <c>CRC16</c> of the serverHash little-endian
    ///     bytes.
    /// </summary>
    public ushort? ClientHash { get; init; }

    /// <summary>Per-packet random data. Null -> generated per encode.</summary>
    public uint? RandData { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Login;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteString8(Name);
        writer.WriteString8(Password);

        var rand1 = Rand1 ?? (byte)Random.Shared.Next(256);
        var xorKey = XorKey ?? (byte)Random.Shared.Next(256);
        var serverHash = ServerHash ?? DefaultServerHash;
        var clientHash = ClientHash ?? Crc16OfUInt32LeBytes(serverHash);
        var randData = RandData ?? (uint)Random.Shared.Next();
        var xorKeyWire = (byte)(((rand1 + Rand1Offset) & 0xFF) ^ xorKey);

        Span<byte> trailer = stackalloc byte[15];
        trailer[0] = rand1;
        trailer[1] = xorKeyWire;
        WriteUInt32BeXored(trailer.Slice(2, 4), serverHash, xorKey, ServerHashMaskBase);
        WriteUInt16BeXored(trailer.Slice(6, 2), clientHash, xorKey, ClientHashMaskBase);
        WriteUInt32BeXored(trailer.Slice(8, 4), randData, xorKey, RandDataMaskBase);

        // CRC is computed over the 12 already-XOR'd trailer bytes (rand1 through randData_xored).
        var crc = CrcCcitt.Compute(trailer[..12]);
        WriteUInt16BeXored(trailer.Slice(12, 2), crc, xorKey, CrcMaskBase);

        trailer[14] = FixedTrailerMarker;

        writer.WriteBytes(trailer);
    }

    /// <summary>
    ///     Parses a 0x03 Login body, de-XORing the integrity trailer and validating the CRC.
    /// </summary>
    public static LoginPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var name = reader.ReadString8();
        var password = reader.ReadString8();

        // Read the 15-byte trailer as a contiguous span so we can both CRC-check the XOR'd
        // bytes and decode the individual fields.
        var trailer = reader.ReadBytes(15);

        var rand1 = trailer[0];
        var xorKeyWire = trailer[1];
        var xorKey = (byte)(((rand1 + Rand1Offset) & 0xFF) ^ xorKeyWire);

        var serverHash = ReadUInt32BeXored(trailer.Slice(2, 4), xorKey, ServerHashMaskBase);
        var clientHash = ReadUInt16BeXored(trailer.Slice(6, 2), xorKey, ClientHashMaskBase);
        var randData = ReadUInt32BeXored(trailer.Slice(8, 4), xorKey, RandDataMaskBase);
        var crc = ReadUInt16BeXored(trailer.Slice(12, 2), xorKey, CrcMaskBase);

        var expectedCrc = CrcCcitt.Compute(trailer[..12]);
        if (crc != expectedCrc)
            throw new InvalidDataException(
                $"0x03 Login: CRC mismatch (expected 0x{expectedCrc:X4}, got 0x{crc:X4}).");

        if (trailer[14] != FixedTrailerMarker)
            throw new InvalidDataException(
                $"0x03 Login: trailer marker mismatch (expected 0x{FixedTrailerMarker:X2}, " +
                $"got 0x{trailer[14]:X2}).");

        return new LoginPacket
        {
            Name = name,
            Password = password,
            Rand1 = rand1,
            XorKey = xorKey,
            ServerHash = serverHash,
            ClientHash = clientHash,
            RandData = randData,
        };
    }

    private static ushort Crc16OfUInt32LeBytes(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        bytes[0] = (byte)(value & 0xFF);
        bytes[1] = (byte)((value >> 8) & 0xFF);
        bytes[2] = (byte)((value >> 16) & 0xFF);
        bytes[3] = (byte)((value >> 24) & 0xFF);

        return CrcCcitt.Compute(bytes);
    }

    private static void WriteUInt32BeXored(Span<byte> dest, uint value, byte xorKey, byte maskBase)
    {
        // Wire BE: dest[0] = msb, dest[3] = lsb. Mask order on the wire is reversed:
        //   dest[0] ^= xor + base + 3   (highest byte gets highest mask offset)
        //   dest[3] ^= xor + base + 0
        dest[0] = (byte)(((value >> 24) & 0xFF) ^ (byte)(xorKey + maskBase + 3));
        dest[1] = (byte)(((value >> 16) & 0xFF) ^ (byte)(xorKey + maskBase + 2));
        dest[2] = (byte)(((value >> 8) & 0xFF) ^ (byte)(xorKey + maskBase + 1));
        dest[3] = (byte)((value & 0xFF) ^ (byte)(xorKey + maskBase + 0));
    }

    private static void WriteUInt16BeXored(Span<byte> dest, ushort value, byte xorKey, byte maskBase)
    {
        dest[0] = (byte)(((value >> 8) & 0xFF) ^ (byte)(xorKey + maskBase + 1));
        dest[1] = (byte)((value & 0xFF) ^ (byte)(xorKey + maskBase + 0));
    }

    private static uint ReadUInt32BeXored(ReadOnlySpan<byte> src, byte xorKey, byte maskBase)
    {
        var b0 = (uint)(src[0] ^ (byte)(xorKey + maskBase + 3));
        var b1 = (uint)(src[1] ^ (byte)(xorKey + maskBase + 2));
        var b2 = (uint)(src[2] ^ (byte)(xorKey + maskBase + 1));
        var b3 = (uint)(src[3] ^ (byte)(xorKey + maskBase + 0));

        return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
    }

    private static ushort ReadUInt16BeXored(ReadOnlySpan<byte> src, byte xorKey, byte maskBase)
    {
        var b0 = (ushort)(src[0] ^ (byte)(xorKey + maskBase + 1));
        var b1 = (ushort)(src[1] ^ (byte)(xorKey + maskBase + 0));

        return (ushort)((b0 << 8) | b1);
    }
}
