using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x29 (S->C) - play a spell/particle effect, either attached to a target creature or over a map
///     tile. The body has two forms, selected by whether the leading serial is zero:
///     <list type="bullet">
///         <item>
///             <description>
///                 <strong>Targeted</strong> (<see cref="TargetId" /> != 0):
///                 <c>[u32 TargetId][u32 SourceId][u16 TargetAnimation][u16 SourceAnimation][u16 Speed]</c>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Area</strong> (<see cref="TargetId" /> == 0):
///                 <c>[u32 0][u16 TargetAnimation][u16 Speed][u16 X][u16 Y]</c>.
///             </description>
///         </item>
///     </list>
///     <see cref="TargetAnimation" /> carries the effect animation in both forms. In the targeted form
///     the source animation renders only when <see cref="SourceAnimation" /> is non-zero.
/// </summary>
[ServerOpcode(ServerOpcode.SpellAnimation)]
public sealed record SpellAnimationPacket : ServerPacket
{
    /// <summary>The target creature's serial, or 0 to play the effect over a map tile (the area form).</summary>
    public required uint TargetId { get; init; }

    /// <summary>The effect animation to play on the target (or over the tile, in the area form).</summary>
    public required ushort TargetAnimation { get; init; }

    /// <summary>The animation playback speed (milliseconds).</summary>
    public required ushort Speed { get; init; }

    /// <summary>The source creature's serial. Targeted form only (ignored when <see cref="IsAreaEffect" />).</summary>
    public uint SourceId { get; init; }

    /// <summary>The effect animation to play on the source. Targeted form only.</summary>
    public ushort SourceAnimation { get; init; }

    /// <summary>The map X coordinate for the effect. Area form only.</summary>
    public ushort X { get; init; }

    /// <summary>The map Y coordinate for the effect. Area form only.</summary>
    public ushort Y { get; init; }

    /// <summary>True when this is the area form (effect over a tile rather than attached to a target).</summary>
    public bool IsAreaEffect => TargetId == 0;

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.SpellAnimation;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(TargetId);

        if (IsAreaEffect)
        {
            writer.WriteUInt16(TargetAnimation);
            writer.WriteUInt16(Speed);
            writer.WriteUInt16(X);
            writer.WriteUInt16(Y);
        }
        else
        {
            writer.WriteUInt32(SourceId);
            writer.WriteUInt16(TargetAnimation);
            writer.WriteUInt16(SourceAnimation);
            writer.WriteUInt16(Speed);
        }
    }

    /// <inheritdoc />
    public static SpellAnimationPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var targetId = reader.ReadUInt32();

        if (targetId == 0)
        {
            var areaAnimation = reader.ReadUInt16();
            var areaSpeed = reader.ReadUInt16();
            var x = reader.ReadUInt16();
            var y = reader.ReadUInt16();

            return new SpellAnimationPacket
            {
                TargetId = 0,
                TargetAnimation = areaAnimation,
                Speed = areaSpeed,
                X = x,
                Y = y,
            };
        }

        var sourceId = reader.ReadUInt32();
        var targetAnimation = reader.ReadUInt16();
        var sourceAnimation = reader.ReadUInt16();
        var speed = reader.ReadUInt16();

        return new SpellAnimationPacket
        {
            TargetId = targetId,
            SourceId = sourceId,
            TargetAnimation = targetAnimation,
            SourceAnimation = sourceAnimation,
            Speed = speed,
        };
    }
}
