using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x00 Version - the first C->S packet sent on a fresh lobby connection, carrying the
///     client version number followed by a fixed two-byte signature.
/// </summary>
/// <remarks>
///     Wire body (after the codec strips the opcode and the universal trailing <c>0x00</c>):
///     <c>[u16-BE version][0x4C 'L'][0x4B 'K']</c>. The full None-mode C->S frame for the default
///     7.41 version is therefore <c>AA 00 06 00 02 E5 4C 4B 00</c>. The trailing <c>0x00</c> is the
///     universal C->S framing null added by the codec; it is <em>not</em> part of this body.
/// </remarks>
[ClientOpcode(ClientOpcode.Version)]
public sealed record VersionPacket : ClientPacket
{
    /// <summary>The 7.41 client version (<c>741</c> decimal, <c>0x02E5</c>).</summary>
    public const ushort Da741 = 0x02E5;

    // The fixed signature that follows the version on the wire: 'L', 'K'.
    private const byte SignatureFirst = 0x4C;  // 'L'
    private const byte SignatureSecond = 0x4B; // 'K'

    /// <summary>
    ///     The client version number, sent big-endian. Defaults to <see cref="Da741" />;
    ///     override only to probe a server's version handling.
    /// </summary>
    public ushort Version { get; init; } = Da741;

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Version;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(Version);
        writer.WriteByte(SignatureFirst);
        writer.WriteByte(SignatureSecond);
    }

    /// <summary>
    ///     Parses a 0x00 Version body. Validates the fixed <c>LK</c> signature and throws
    ///     <see cref="InvalidDataException" /> on mismatch (fail early).
    /// </summary>
    public static VersionPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var version = reader.ReadUInt16();
        var first = reader.ReadByte();
        var second = reader.ReadByte();

        if (first != SignatureFirst || second != SignatureSecond)
            throw new InvalidDataException(
                "0x00 Version: expected 'LK' signature " +
                $"(0x{SignatureFirst:X2} 0x{SignatureSecond:X2}), got 0x{first:X2} 0x{second:X2}.");

        return new VersionPacket { Version = version };
    }
}
