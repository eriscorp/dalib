using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x6B (S->C) - a one-byte signal that opens the in-game town/world-map window. The body is a
///     single <c>[u8]</c> that most likely selects which map to show.
///     Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
[ServerOpcode(ServerOpcode.Screenshot)]
public sealed record ScreenshotPacket : ServerPacket
{
    /// <summary>The single body byte. Meaning unverified; preserved verbatim.</summary>
    public required byte Value { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Screenshot;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Value);

    /// <inheritdoc />
    public static ScreenshotPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new ScreenshotPacket
        {
            Value = reader.ReadByte(),
        };
    }
}
