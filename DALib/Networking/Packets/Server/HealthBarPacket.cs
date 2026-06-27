using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x13 (S->C) - update the floating health bar over a creature/user and optionally play a hit
///     sound. The body is <c>[u32 BE SourceId][u8 0][u8 HealthPercent][u8 Sound]</c>.
/// </summary>
/// <remarks>
///     <see cref="HealthPercent" /> is 0-100 and scales the bar. <see cref="Sound" /> is a sound-effect
///     index played on the update; <c>0xFF</c> means "no sound". The single zero byte between the serial
///     and the percent is a constant, exposed as <see cref="Unknown" /> for byte-faithful round-trips.
/// </remarks>
[ServerOpcode(ServerOpcode.HealthBar)]
public sealed record HealthBarPacket : ServerPacket
{
    /// <summary>The serial of the creature/user whose health bar is being updated.</summary>
    public required uint SourceId { get; init; }

    /// <summary>The health percentage (0-100); scales the bar. 0 hides the bar.</summary>
    public required byte HealthPercent { get; init; }

    /// <summary>Sound-effect index played on the update; <c>0xFF</c> = no sound. Defaults to no sound.</summary>
    public byte Sound { get; init; } = 0xFF;

    /// <summary>The constant zero byte emitted before the percent. Settable round-trip field.</summary>
    public byte Unknown { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.HealthBar;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(SourceId);
        writer.WriteByte(Unknown);
        writer.WriteByte(HealthPercent);
        writer.WriteByte(Sound);
    }

    /// <inheritdoc />
    public static HealthBarPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var sourceId = reader.ReadUInt32();
        var unknown = reader.ReadByte();
        var healthPercent = reader.ReadByte();
        var sound = reader.ReadByte();

        return new HealthBarPacket
        {
            SourceId = sourceId,
            HealthPercent = healthPercent,
            Sound = sound,
            Unknown = unknown,
        };
    }
}
