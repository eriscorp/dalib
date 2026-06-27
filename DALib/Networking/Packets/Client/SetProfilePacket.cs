using System;
using System.Text;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x4F (C->S) - the player uploads their own profile: a portrait image plus the profile text
///     shown on the legend/profile pane. Body:
///     <c>[u16 totalLength][u16 portraitLength][portrait bytes][string16 ProfileText]</c>.
/// </summary>
/// <remarks>
///     <see cref="PortraitData" /> is the raw portrait image bytes, treated as an opaque byte array;
///     <c>portraitLength</c> is derived from its length on write. <c>totalLength</c> is a redundant
///     length-of-rest: DALib writes it as the byte count of everything after the totalLength field
///     (portraitLength + portrait bytes + the string16 message with its length prefix), and reads
///     and discards it on parse.
/// </remarks>
[ClientOpcode(ClientOpcode.SetProfile)]
public sealed record SetProfilePacket : ClientPacket
{
    /// <summary>Raw portrait image bytes. Opaque to DALib; may be empty.</summary>
    public required byte[] PortraitData { get; init; }

    /// <summary>The profile / legend text. Length-prefixed with a 16-bit count on the wire.</summary>
    public required string ProfileText { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.SetProfile;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (PortraitData.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"{nameof(SetProfilePacket)}: portrait length {PortraitData.Length} exceeds the wire " +
                $"u16 limit.");

        // totalLength = bytes after this field: [u16 portraitLength] + portrait + [u16 msgLen] + msg.
        var messageByteCount = Encoding.Latin1.GetByteCount(ProfileText);
        var totalLength = 2 + PortraitData.Length + 2 + messageByteCount;

        if (totalLength > ushort.MaxValue)
            throw new InvalidOperationException(
                $"{nameof(SetProfilePacket)}: total profile length {totalLength} exceeds the wire " +
                $"u16 limit.");

        writer.WriteUInt16((ushort)totalLength);
        writer.WriteUInt16((ushort)PortraitData.Length);
        writer.WriteBytes(PortraitData);
        writer.WriteString16(ProfileText);
    }

    /// <inheritdoc />
    public static SetProfilePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        _ = reader.ReadUInt16(); // totalLength - redundant, not validated.
        var portraitLength = reader.ReadUInt16();
        var portraitData = reader.ReadBytes(portraitLength).ToArray();
        var profileText = reader.ReadString16();

        return new SetProfilePacket
        {
            PortraitData = portraitData,
            ProfileText = profileText,
        };
    }
}
