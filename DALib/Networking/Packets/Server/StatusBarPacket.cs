using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x3A (S->C) - add, recolor, or remove a status-effect icon on the player's status bar (the buff
///     /debuff row). The body is <c>[u16 Icon][u8 Color]</c>. <see cref="Icon" /> is the status-effect
///     icon index; <c>0</c> removes the icon. <see cref="Color" /> is a <see cref="StatusBarColor" />
///     duration indicator (values 1-6; 0 removes the icon).
/// </summary>
[ServerOpcode(ServerOpcode.StatusBar)]
public sealed record StatusBarPacket : ServerPacket
{
    /// <summary>The status-effect icon index; <c>0</c> removes the icon.</summary>
    public required ushort Icon { get; init; }

    /// <summary>The icon color (duration indicator); <see cref="StatusBarColor.None" /> removes the icon.</summary>
    public required StatusBarColor Color { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.StatusBar;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(Icon);
        writer.WriteByte((byte)Color);
    }

    /// <inheritdoc />
    public static StatusBarPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var icon = reader.ReadUInt16();
        var color = (StatusBarColor)reader.ReadByte();

        return new StatusBarPacket
        {
            Icon = icon,
            Color = color,
        };
    }
}
