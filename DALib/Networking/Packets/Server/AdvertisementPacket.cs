using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x5B (S->C) - an advertisement push. Body: a <c>u16</c>-length-prefixed raw byte blob
///     followed by two <c>u16</c>s and a <c>u8</c>: <c>[u16 len][len bytes][u16][u16][u8]</c> (the
///     <c>u16</c>s are big-endian). The blob is read as raw bytes, not a length-prefixed string.
///     Modeled for protocol completeness; not emitted by typical servers. The trailing
///     <c>Unknown*</c> members are preserved for faithful round-tripping.
/// </summary>
[ServerOpcode(ServerOpcode.Advertisement)]
public sealed record AdvertisementPacket : ServerPacket
{
    /// <summary>The advertisement content: a <c>u16</c>-length-prefixed raw byte blob. Defaults to empty.</summary>
    public byte[] Data { get; init; } = [];

    /// <summary>The first trailing big-endian <c>u16</c>. Meaning unknown; preserved verbatim.</summary>
    public required ushort Unknown1 { get; init; }

    /// <summary>The second trailing big-endian <c>u16</c>. Meaning unknown; preserved verbatim.</summary>
    public required ushort Unknown2 { get; init; }

    /// <summary>The trailing <c>u8</c>. Meaning unknown; preserved verbatim.</summary>
    public required byte Unknown3 { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Advertisement;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Data.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"AdvertisementPacket: payload length {Data.Length} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16((ushort)Data.Length);
        writer.WriteBytes(Data);
        writer.WriteUInt16(Unknown1);
        writer.WriteUInt16(Unknown2);
        writer.WriteByte(Unknown3);
    }

    /// <inheritdoc />
    public static AdvertisementPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var length = reader.ReadUInt16();
        var data = reader.ReadBytes(length).ToArray();

        return new AdvertisementPacket
        {
            Data = data,
            Unknown1 = reader.ReadUInt16(),
            Unknown2 = reader.ReadUInt16(),
            Unknown3 = reader.ReadByte(),
        };
    }
}
