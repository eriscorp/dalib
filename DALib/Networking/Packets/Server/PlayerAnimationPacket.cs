using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x1A (S->C) - play a body/motion animation on a creature or user (an assail swing, a bow, a
///     greeting, etc.). The body is <c>[u32 BE SourceId][u8 Animation][u16 Speed]</c>. Some servers
///     (e.g. Hybrasyl) append a trailing slack byte the client does not read; Parse tolerates it.
///     Distinct from 0x29 <see cref="SpellAnimationPacket" />, which plays particle/spell effects rather
///     than body motions.
/// </summary>
[ServerOpcode(ServerOpcode.PlayerAnimation)]
public sealed record PlayerAnimationPacket : ServerPacket
{
    /// <summary>The serial of the creature/user performing the animation.</summary>
    public required uint SourceId { get; init; }

    /// <summary>The body-animation index to play.</summary>
    public required byte Animation { get; init; }

    /// <summary>The animation playback speed (milliseconds).</summary>
    public required ushort Speed { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.PlayerAnimation;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(SourceId);
        writer.WriteByte(Animation);
        writer.WriteUInt16(Speed);
    }

    /// <inheritdoc />
    public static PlayerAnimationPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var sourceId = reader.ReadUInt32();
        var animation = reader.ReadByte();
        var speed = reader.ReadUInt16();

        // Any remaining bytes are emitter slack (e.g. Hybrasyl's 0xFF) or DOOMVAS inner-pad;
        // the client does not read past Speed, so neither is modeled. Tolerated, not rejected.

        return new PlayerAnimationPacket
        {
            SourceId = sourceId,
            Animation = animation,
            Speed = speed,
        };
    }
}
