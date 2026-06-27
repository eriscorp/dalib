using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x03 (S->C) - directs a reconnect to a different server, supplying the new connection's
///     seed/key plus a server-side validation token. Used at lobby->login, login->world, and
///     world->login (logoff/error) transitions.
/// </summary>
/// <remarks>
///     The seed/key/name/id following the port are server-to-server data carried through the
///     connection unchanged; consumers building servers construct them for the redirect target.
/// </remarks>
[ServerOpcode(ServerOpcode.Redirect)]
public sealed record RedirectPacket : ServerPacket
{
    /// <summary>
    ///     Destination IPv4 address. On the wire the four octets are emitted in
    ///     <strong>reverse</strong> of network order (127.0.0.1 -> <c>01 00 00 7F</c>), which
    ///     differs from <see cref="ServerEntry.IpAddress" />'s network-order convention.
    /// </summary>
    public required IPAddress IpAddress { get; init; }

    /// <summary>Destination TCP port (big-endian on the wire).</summary>
    public required ushort Port { get; init; }

    /// <summary>Encryption seed for the new connection.</summary>
    public required byte EncryptionSeed { get; init; }

    /// <summary>
    ///     Encryption key for the new connection. Length-prefixed on the wire by a u8; max
    ///     <see cref="byte.MaxValue" /> bytes.
    /// </summary>
    public required byte[] EncryptionKey { get; init; }

    /// <summary>
    ///     Account / character name passed to the redirect target. At lobby->login this is a
    ///     placeholder; at login->world it's the chosen character name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Server-side validation token. The redirect target verifies this against its
    ///     registration manifest to confirm the incoming connection is the expected one.
    /// </summary>
    public required uint RedirectId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Redirect;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (IpAddress.AddressFamily != AddressFamily.InterNetwork)
            throw new InvalidOperationException(
                $"Redirect: non-IPv4 address {IpAddress} (family {IpAddress.AddressFamily}); " +
                "only IPv4 is supported on the wire.");

        if (EncryptionKey.Length > byte.MaxValue)
            throw new InvalidOperationException(
                $"Redirect: encryption key length {EncryptionKey.Length} exceeds wire u8 limit.");

        var nameBytes = System.Text.Encoding.Latin1.GetByteCount(Name);
        if (nameBytes > byte.MaxValue)
            throw new InvalidOperationException(
                $"Redirect: name length {nameBytes} exceeds wire u8 limit.");

        var innerLength = EncryptionKey.Length + nameBytes + 7;
        if (innerLength > byte.MaxValue)
            throw new InvalidOperationException(
                $"Redirect: inner length {innerLength} exceeds wire u8 limit.");

        var addressBytes = IpAddress.GetAddressBytes();
        Array.Reverse(addressBytes);
        writer.WriteBytes(addressBytes);

        writer.WriteUInt16(Port);
        writer.WriteByte((byte)innerLength);
        writer.WriteByte(EncryptionSeed);
        writer.WriteByte((byte)EncryptionKey.Length);
        writer.WriteBytes(EncryptionKey);
        writer.WriteString8(Name);
        writer.WriteUInt32(RedirectId);
    }

    /// <inheritdoc />
    public static RedirectPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var addressBytes = reader.ReadBytes(4).ToArray();
        Array.Reverse(addressBytes);
        var ipAddress = new IPAddress(addressBytes);

        var port = reader.ReadUInt16();
        _ = reader.ReadByte(); // innerLength -- structurally redundant, value derivable from the fields below
        var seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        var key = reader.ReadBytes(keyLength).ToArray();
        var name = reader.ReadString8();
        var redirectId = reader.ReadUInt32();

        return new RedirectPacket
        {
            IpAddress = ipAddress,
            Port = port,
            EncryptionSeed = seed,
            EncryptionKey = key,
            Name = name,
            RedirectId = redirectId,
        };
    }
}
