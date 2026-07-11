using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x42 (C->S) - report a client exception/crash. Body
///     <c>[u8 0x01 gate][u16 BE dataLen][bytes Data][u8 0x00]</c>: the client uploads the contents of its
///     crash log (<c>LCrash.nfo</c>) or an error string as an opaque, null-terminated blob.
/// </summary>
/// <remarks>
///     Client diagnostics infrastructure, not a game action - the retail client sends this on a crash and
///     then deletes the log. Binary-verified (two send sites, file-based and string-based); not emitted by
///     Hybrasyl or Chaos. Uses Normal encryption. <see cref="Data" /> is carried verbatim; DALib does not
///     interpret it.
/// </remarks>
[ClientOpcode(ClientOpcode.Exception)]
public sealed record ExceptionPacket : ClientPacket
{
    /// <summary>The hard-coded gate byte that leads the body.</summary>
    public const byte GateByte = 0x01;

    /// <summary>The crash-report payload, carried opaque. Length-prefixed by a <c>u16</c>, so at most 65535
    ///     bytes.</summary>
    public byte[] Data { get; init; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Exception;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Data.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"0x42 Exception: data length {Data.Length} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteByte(GateByte);
        writer.WriteUInt16((ushort)Data.Length);
        writer.WriteBytes(Data);
        writer.WriteByte(0x00); // trailing null terminator
    }

    /// <inheritdoc />
    public static ExceptionPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var gate = reader.ReadByte();

        if (gate != GateByte)
            throw new InvalidDataException(
                $"0x42 Exception: expected gate byte 0x{GateByte:X2}, got 0x{gate:X2}.");

        var dataLen = reader.ReadUInt16();

        return new ExceptionPacket { Data = reader.ReadBytes(dataLen).ToArray() };
    }
}
