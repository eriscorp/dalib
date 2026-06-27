using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x3B (S->C) - the byte-heartbeat challenge. The body is two random bytes,
///     <c>[u8 First][u8 Second]</c>; the reply echoes them back (reversed) via C->S 0x45
///     (<see cref="DALib.Networking.Packets.Client.ByteHeartbeatPacket" />), and the session is dropped
///     if the reply does not match.
/// </summary>
/// <remarks>
///     The byte values are an opaque challenge: recorded on send and validated against the reversed echo.
///     (0x3B is also a C->S opcode, but the separate ServerOpcode/ClientOpcode enums make the direction
///     explicit.)
/// </remarks>
[ServerOpcode(ServerOpcode.ByteHeartbeat)]
public sealed record ByteHeartbeatPacket : ServerPacket
{
    /// <summary>The first challenge byte on the wire.</summary>
    public required byte First { get; init; }

    /// <summary>The second challenge byte on the wire.</summary>
    public required byte Second { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.ByteHeartbeat;

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
