using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x0D (S->C) - a public spoken message broadcast to nearby players, rendered as an overhead bubble
///     above the speaker and a line in the chat log. The body is
///     <c>[u8 Type][u32 BE SourceId][string8 Message]</c>. <see cref="SourceId" /> is the speaker's serial,
///     used to position the overhead bubble.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Type" /> selects how the line renders. Three values are known:
///         <see cref="TypeSay" /> (0, normal white "Name: text"), <see cref="TypeShout" /> (1, yellow
///         "Name! text"), and <see cref="TypeChant" /> (2, blue - the word-by-word spell-chant echo sent
///         back for each 0x4E cast-line). Guild/group chat go out as 0x0A Message rather than here.
///     </para>
///     <para>
///         The full set of distinct render types is not enumerated, so <see cref="Type" /> is a plain
///         <see cref="byte" /> (not an enum) so any value round-trips faithfully.
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.PublicMessage)]
public sealed record PublicMessagePacket : ServerPacket
{
    /// <summary>0 - normal local say (white), heard by nearby players. Rendered "Name: text".</summary>
    public const byte TypeSay = 0x00;

    /// <summary>1 - a shout (yellow), heard map-wide. Rendered "Name! text".</summary>
    public const byte TypeShout = 0x01;

    /// <summary>2 - spell-chant echo (blue): one word per packet as the caster speaks the chant.</summary>
    public const byte TypeChant = 0x02;

    /// <summary>The render type. See the <c>Type*</c> constants for the known values.</summary>
    public required byte Type { get; init; }

    /// <summary>The speaker's serial; the overhead bubble is positioned from it.</summary>
    public required uint SourceId { get; init; }

    /// <summary>The message text (Latin-1), already formatted by the server (e.g. "Name: hello").</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.PublicMessage;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteUInt32(SourceId);
        writer.WriteString8(Message);
    }

    /// <inheritdoc />
    public static PublicMessagePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var type = reader.ReadByte();
        var sourceId = reader.ReadUInt32();
        var message = reader.ReadString8();

        return new PublicMessagePacket
        {
            Type = type,
            SourceId = sourceId,
            Message = message,
        };
    }
}
