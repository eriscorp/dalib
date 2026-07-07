using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x56 (S->C) - lobby response carrying the full server table, zlib-compressed. Sent in
///     reply to <see cref="DALib.Networking.Packets.Client.ServerTableRequestPacket" />.
/// </summary>
/// <remarks>
///     The per-entry description field exists only in the local <c>mServer.tbl</c> cache and is
///     never on the wire - see <see cref="ServerEntry" />.
/// </remarks>
[ServerOpcode(ServerOpcode.ServerTableData)]
public sealed record ServerTableDataPacket : ServerPacket
{
    /// <summary>
    ///     The advertised servers. Wire-order is preserved.
    /// </summary>
    public required IList<ServerEntry> Servers { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.ServerTableData;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Servers.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"ServerTableData: server count {Servers.Count} exceeds wire u8 limit (255).");

        // Build the plaintext payload, then compress.
        var plain = new PacketWriter();
        plain.WriteByte((byte)Servers.Count);

        foreach (var server in Servers)
            WriteEntry(plain, server);

        var compressed = Compress(plain.WrittenSpan);

        if (compressed.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"ServerTableData: compressed payload {compressed.Length} bytes exceeds " +
                "wire u16 limit (65535).");

        writer.WriteUInt16((ushort)compressed.Length);
        writer.WriteBytes(compressed);
    }

    /// <summary>
    ///     Parses a 0x56 body - reads the compressed-length prefix, inflates the DEFLATE payload
    ///     (see <see cref="Decompress" /> for why the Adler-32 trailer is not validated), then
    ///     unpacks the server entries.
    /// </summary>
    public static ServerTableDataPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var compressedLength = reader.ReadUInt16();
        var compressedSpan = reader.ReadBytes(compressedLength);

        var inflated = Decompress(compressedSpan);
        var inflatedReader = new PacketReader(inflated);

        var serverCount = inflatedReader.ReadByte();
        var servers = new List<ServerEntry>(serverCount);

        for (var i = 0; i < serverCount; i++)
            servers.Add(ReadEntry(ref inflatedReader));

        return new ServerTableDataPacket { Servers = servers };
    }

    private static void WriteEntry(IPacketWriter writer, ServerEntry server)
    {
        if (server.IpAddress.AddressFamily != AddressFamily.InterNetwork)
            throw new InvalidOperationException(
                $"ServerTableData: server '{server.Name}' has non-IPv4 address " +
                $"{server.IpAddress} (family {server.IpAddress.AddressFamily}); " +
                "only IPv4 is supported on the wire.");

        writer.WriteByte(server.Id);
        writer.WriteBytes(server.IpAddress.GetAddressBytes());
        writer.WriteUInt16(server.Port);
        writer.WriteCString(server.Name);
    }

    private static ServerEntry ReadEntry(ref PacketReader reader)
    {
        var id = reader.ReadByte();
        var ipAddress = new IPAddress(reader.ReadBytes(4));
        var port = reader.ReadUInt16();
        var name = reader.ReadCString();

        return new ServerEntry
        {
            Id = id,
            IpAddress = ipAddress,
            Port = port,
            Name = name,
        };
    }

    private static byte[] Compress(ReadOnlySpan<byte> plain)
    {
        using var output = new MemoryStream();

        using (var deflater = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            deflater.Write(plain);

        return output.ToArray();
    }

    // A zlib stream is [2-byte header][raw DEFLATE][4-byte Adler-32 trailer]. We inflate the
    // DEFLATE body directly rather than validating the full zlib envelope: some DOOMVAS servers
    // emit a bogus Adler-32 (e.g. Hybrasyl computes it over an empty buffer, always writing
    // 0x00000001), and the retail client - like the servers' own decompressors - never checks it.
    // Validating here would reject an otherwise-decodable table, so we match the ecosystem and skip
    // the trailer. Compress still writes a correct zlib stream, so this stays round-trip clean.
    private const int ZlibHeaderLength = 2;
    private const int Adler32TrailerLength = 4;

    private static byte[] Decompress(ReadOnlySpan<byte> compressed)
    {
        if (compressed.Length < ZlibHeaderLength + Adler32TrailerLength)
            throw new InvalidDataException(
                $"ServerTableData: compressed payload is {compressed.Length} bytes, too short to be a "
                + "zlib stream (2-byte header + 4-byte trailer).");

        var deflateBody = compressed.Slice(
            ZlibHeaderLength,
            compressed.Length - ZlibHeaderLength - Adler32TrailerLength);

        using var input = new MemoryStream(deflateBody.ToArray(), writable: false);
        using var inflater = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        inflater.CopyTo(output);

        return output.ToArray();
    }
}
