using System;
using System.Text;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x7E (S->C) - the lobby's connection greeting, sent unencrypted the moment a client connects
///     (before the 0x00 CryptoKey handshake). Body: <c>[u8 Marker = 0x1B]["CONNECTED SERVER"]</c>. It
///     acknowledges that the client has reached a server; the payload is not acted upon. 0x7E is exempt
///     from encryption (<c>EncryptMethod.None</c>, alongside 0x00/0x03); DALib models the message as the
///     rest of the packet after the marker and emits it with no terminator. The <see cref="Marker" />
///     byte's meaning is unknown (it is not the string length: 0x1B = 27, the banner is 16 chars), so it
///     is modeled as an opaque field with a default.
/// </summary>
[ServerOpcode(ServerOpcode.AcceptConnection)]
public sealed record AcceptConnectionPacket : ServerPacket
{
    /// <summary>The fixed leading marker byte (meaning unknown; not a length).</summary>
    public byte Marker { get; init; } = 0x1B;

    /// <summary>The banner string that fills the rest of the body.</summary>
    public string Message { get; init; } = "CONNECTED SERVER";

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.AcceptConnection;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Marker);
        writer.WriteBytes(Encoding.Latin1.GetBytes(Message));
    }

    /// <inheritdoc />
    public static AcceptConnectionPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var marker = reader.ReadByte();
        var message = Encoding.Latin1.GetString(reader.Remaining);

        return new AcceptConnectionPacket
        {
            Marker = marker,
            Message = message,
        };
    }
}
