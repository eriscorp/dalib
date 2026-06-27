using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x00 CryptoKey (S->C, subtype 0x00) - the lobby server's reply to the C->S 0x00 Version,
///     handing over the per-connection encryption seed and key.
/// </summary>
/// <remarks>
///     <para>
///         The S->C 0x00 opcode is multiplexed by a leading <em>subtype</em> byte (body offset 0):
///         <list type="bullet">
///             <item><c>0x00</c> - CryptoKey (this type): seed + key + server-table CRC.</item>
///             <item><c>0x01</c> - Notice/Error: a canned message dialog.</item>
///             <item><c>0x02</c> - Patch directive: an auto-updater run.</item>
///         </list>
///         Notice (0x01) and Patch (0x02) are recognized but not modeled - <see cref="Parse" />
///         throws for them.
///     </para>
///     <para>
///         Subtype-0 body:
///         <c>[0x00 subtype][u32-BE serverTableCrc][byte seed][byte keyLen][key x keyLen]</c>.
///         No encryption (the codec frames S->C None without a trailing null).
///         <see cref="ServerTableCrc" /> is compared against a CRC of the cached server table;
///         a mismatch drives a 0x57 request with <c>mismatch=1</c> to pull the full table.
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.CryptoKey)]
public sealed record CryptoKeyPacket : ServerPacket
{
    /// <summary>The subtype byte that selects the CryptoKey variant of the 0x00 opcode.</summary>
    public const byte SubtypeCryptoKey = 0x00;

    /// <summary>
    ///     CRC of the server's current server table. Compared against the cached table CRC;
    ///     a mismatch drives a request for the full table (0x57 mismatch=1).
    /// </summary>
    public required uint ServerTableCrc { get; init; }

    /// <summary>The encryption seed (salt-table selector) for this connection.</summary>
    public required byte Seed { get; init; }

    /// <summary>The encryption key (9 bytes in practice); its length is wire-prefixed.</summary>
    public required byte[] Key { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.CryptoKey;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(SubtypeCryptoKey);
        writer.WriteUInt32(ServerTableCrc);
        writer.WriteByte(Seed);
        writer.WriteByte((byte)Key.Length);
        writer.WriteBytes(Key);
    }

    /// <summary>
    ///     Parses a subtype-0 (CryptoKey) 0x00 body. Throws <see cref="InvalidDataException" />
    ///     for the Notice (0x01) and Patch (0x02) subtypes, which are recognized but unmodeled.
    /// </summary>
    public static CryptoKeyPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var subtype = reader.ReadByte();

        if (subtype != SubtypeCryptoKey)
            throw new InvalidDataException(
                $"0x00 subtype 0x{subtype:X2} is not CryptoKey (0x00): " +
                "0x01 is Notice, 0x02 is Patch - only CryptoKey is modeled.");

        var serverTableCrc = reader.ReadUInt32();
        var seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        var key = reader.ReadBytes(keyLength).ToArray();

        return new CryptoKeyPacket
        {
            ServerTableCrc = serverTableCrc,
            Seed = seed,
            Key = key,
        };
    }
}
