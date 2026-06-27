using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x20 (S->C) - set the ambient light/darkness level of the current map (the day-night / dungeon
///     dimming overlay). The body is <c>[u8 LightLevel]</c>.
/// </summary>
/// <remarks>
///     <see cref="LightLevel" /> is a time-of-day index rather than an abstract brightness: an
///     every-other-hour value in 0-12 (values >= 12 clamp to the 24-hour maximum), affecting outdoor
///     maps only. Modeled as a raw <see cref="byte" /> rather than an enum, since the actual sky tints
///     are resolved from a lighting table and not carried on the wire.
/// </remarks>
[ServerOpcode(ServerOpcode.LightLevel)]
public sealed record LightLevelPacket : ServerPacket
{
    /// <summary>The ambient light level (lower = darker); a discrete step into the lighting palette.</summary>
    public required byte LightLevel { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.LightLevel;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(LightLevel);

    /// <inheritdoc />
    public static LightLevelPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var lightLevel = reader.ReadByte();

        return new LightLevelPacket
        {
            LightLevel = lightLevel,
        };
    }
}
