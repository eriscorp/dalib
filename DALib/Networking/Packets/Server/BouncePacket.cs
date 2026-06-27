using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x4B (S->C) - "bounce": instructs the recipient to send a packet as if it had generated it
///     itself. The body is a length-prefixed inner C->S packet:
///     <c>[u16-BE innerLen][u8 ClientOpcode][bytes Data]</c>, where <c>innerLen = 1 + Data.Length</c>
///     counts the opcode byte plus its data. The inner packet is re-emitted back up to the server.
/// </summary>
/// <remarks>
///     <see cref="Data" /> is the inner packet's body (everything after its opcode byte); the length
///     prefix is derived (<c>1 + Data.Length</c>) on write and consumed on read.
/// </remarks>
[ServerOpcode(ServerOpcode.Bounce)]
public sealed record BouncePacket : ServerPacket
{
    /// <summary>The C->S opcode to send.</summary>
    public required ClientOpcode ClientOpcode { get; init; }

    /// <summary>The body of the forced inner packet - everything after its opcode byte. May be empty.</summary>
    public required byte[] Data { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Bounce;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        var innerLen = Data.Length + 1; // the forced opcode byte plus its data

        if (innerLen > ushort.MaxValue)
            throw new InvalidOperationException(
                $"BouncePacket: inner packet length {innerLen} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16((ushort)innerLen);
        writer.WriteByte((byte)ClientOpcode);
        writer.WriteBytes(Data);
    }

    /// <inheritdoc />
    public static BouncePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var innerLen = reader.ReadUInt16();

        if (innerLen < 1)
            throw new InvalidDataException(
                $"0x4B Bounce: inner length {innerLen} is too small to contain the forced opcode byte.");

        var clientOpcode = (ClientOpcode)reader.ReadByte();
        var data = reader.ReadBytes(innerLen - 1).ToArray();

        return new BouncePacket
        {
            ClientOpcode = clientOpcode,
            Data = data,
        };
    }
}
