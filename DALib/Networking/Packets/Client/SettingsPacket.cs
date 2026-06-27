using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x1B (C->S) - toggle a client setting, or request the full settings list. Body:
///     <c>[u8 SettingNumber]</c>.
/// </summary>
/// <remarks>
///     A <see cref="SettingNumber" /> of 0 requests all current settings (the server replies with
///     the settings dialog); any other value toggles that setting and echoes its new state.
/// </remarks>
[ClientOpcode(ClientOpcode.Settings)]
public sealed record SettingsPacket : ClientPacket
{
    /// <summary>The setting to toggle; 0 requests the full settings list.</summary>
    public required byte SettingNumber { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Settings;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(SettingNumber);

    /// <inheritdoc />
    public static SettingsPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new SettingsPacket
        {
            SettingNumber = reader.ReadByte(),
        };
    }
}
