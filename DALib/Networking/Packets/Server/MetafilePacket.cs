using System;
using System.Collections.Generic;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x6F (S->C) - pushes metafile content. The body opens with a single <c>[u8 type]</c> byte (a
///     <see cref="MetafileType" />) selecting the form: either one named file's compressed data
///     (<see cref="MetafileDataPacket" />, <see cref="MetafileType.DataByName" />) or the checksum
///     manifest for every metafile (<see cref="MetafileChecksumsPacket" />,
///     <see cref="MetafileType.AllCheckSums" />). Answers C->S 0x7B RequestMetafile.
/// </summary>
/// <remarks>
///     The data is the opaque, zlib-compressed blob; the checksum is of the decompressed bytes.
///     This parses its fields and tolerates any framing slack.
/// </remarks>
[ServerOpcode(ServerOpcode.Metafile)]
public abstract record MetafilePacket : ServerPacket
{
    /// <summary>The leading byte that selects this form. Fixed per concrete variant.</summary>
    public abstract MetafileType MetafileType { get; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Metafile;

    /// <summary>
    ///     Parses a 0x6F body, dispatching on the leading <see cref="MetafileType" /> byte to the matching
    ///     variant. This is what <see cref="ServerOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static MetafilePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (MetafileType)reader.ReadByte();

        return type switch
        {
            MetafileType.DataByName => MetafileDataPacket.ParseBody(ref reader),
            MetafileType.AllCheckSums => MetafileChecksumsPacket.ParseBody(ref reader),
            _ => throw new InvalidDataException($"0x6F Metafile: unknown type 0x{(byte)type:X2}.")
        };
    }
}

/// <summary>
///     0x6F type 0 (<see cref="MetafileType.DataByName" />) - one named metafile's content. Body
///     <c>[u8 0x00][string8 Name][u32-BE Checksum][u16-BE DataLen][bytes Data]</c>.
/// </summary>
public sealed record MetafileDataPacket : MetafilePacket
{
    /// <summary>The metafile's name (matches the by-name request that asked for it).</summary>
    public required string Name { get; init; }

    /// <summary>The CRC32 of the (decompressed) metafile content. Round-tripped verbatim.</summary>
    public required uint Checksum { get; init; }

    /// <summary>The raw on-wire metafile payload (the zlib-compressed node stream); decompression is left to the caller.</summary>
    public required byte[] Data { get; init; }

    /// <inheritdoc />
    public override MetafileType MetafileType => MetafileType.DataByName;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Data.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"MetafileDataPacket: payload length {Data.Length} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteByte((byte)MetafileType.DataByName);
        writer.WriteString8(Name);
        writer.WriteUInt32(Checksum);
        writer.WriteUInt16((ushort)Data.Length);
        writer.WriteBytes(Data);
    }

    internal static MetafileDataPacket ParseBody(ref PacketReader reader)
    {
        var name = reader.ReadString8();
        var checksum = reader.ReadUInt32();
        var length = reader.ReadUInt16();

        return new MetafileDataPacket
        {
            Name = name,
            Checksum = checksum,
            Data = reader.ReadBytes(length).ToArray()
        };
    }
}

/// <summary>One entry of a <see cref="MetafileChecksumsPacket" /> manifest: a metafile's name and checksum.</summary>
public readonly record struct MetafileEntry(string Name, uint Checksum);

/// <summary>
///     0x6F type 1 (<see cref="MetafileType.AllCheckSums" />) - the checksum manifest for every metafile.
///     Body <c>[u8 0x01][u16-BE Count]{[string8 Name][u32-BE Checksum]}</c>. Each entry whose checksum
///     differs from the cached copy is re-requested by name (C->S 0x7B).
/// </summary>
public sealed record MetafileChecksumsPacket : MetafilePacket
{
    /// <summary>
    ///     The per-metafile name+checksum entries. Mutable so a server can accumulate them while building
    ///     the manifest.
    /// </summary>
    public IList<MetafileEntry> Entries { get; set; } = [];

    /// <inheritdoc />
    public override MetafileType MetafileType => MetafileType.AllCheckSums;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Entries.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"MetafileChecksumsPacket: entry count {Entries.Count} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteByte((byte)MetafileType.AllCheckSums);
        writer.WriteUInt16((ushort)Entries.Count);

        foreach (var entry in Entries)
        {
            writer.WriteString8(entry.Name);
            writer.WriteUInt32(entry.Checksum);
        }
    }

    internal static MetafileChecksumsPacket ParseBody(ref PacketReader reader)
    {
        var count = reader.ReadUInt16();
        var entries = new List<MetafileEntry>(count);

        for (var i = 0; i < count; i++)
            entries.Add(new MetafileEntry(reader.ReadString8(), reader.ReadUInt32()));

        return new MetafileChecksumsPacket { Entries = entries };
    }
}
