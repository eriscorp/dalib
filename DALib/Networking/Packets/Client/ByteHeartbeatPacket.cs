using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x45 (C-&gt;S) - reply to the server's byte-heartbeat challenge. Body: <c>[u8 First][u8 Second]</c>.
/// </summary>
/// <remarks>
///     The server periodically sends a two-byte challenge; the reply echoes those two bytes back in
///     reverse order, and the server disconnects the session if the reply does not match the challenge
///     it issued. <see cref="First" /> and <see cref="Second" /> are the bytes in wire order:
///     <see cref="First" /> is the second challenge byte and <see cref="Second" /> is the first.
/// </remarks>
[ClientOpcode(ClientOpcode.ByteHeartbeat)]
public sealed record ByteHeartbeatPacket : ClientPacket
{
    /// <summary>First byte on the wire (the server challenge's second byte).</summary>
    public required byte First { get; init; }

    /// <summary>Second byte on the wire (the server challenge's first byte).</summary>
    public required byte Second { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.ByteHeartbeat;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(First);
        writer.WriteByte(Second);
    }

    /// <inheritdoc />
    public static ByteHeartbeatPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var first = reader.ReadByte();
        var second = reader.ReadByte();

        return new ByteHeartbeatPacket
        {
            First = first,
            Second = second,
        };
    }
}
