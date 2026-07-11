using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x31 (C->S) - confirm a server prompt. Body <c>[u8 Arg0][u8 Arg1][u8 Arg2][u8 len][bytes Payload]</c>:
///     three context bytes then a single-byte-length-prefixed payload blob.
/// </summary>
/// <remarks>
///     Binary-verified send; not emitted by Hybrasyl or Chaos (LoD-lineage). The three leading bytes come
///     from the prompt pane's state (<see cref="Arg1" /> is the caller-supplied value); their exact roles
///     are not pinned to the binary. Modeled for wire completeness with faithful structure.
/// </remarks>
[ClientOpcode(ClientOpcode.Confirm)]
public sealed record ConfirmPacket : ClientPacket
{
    /// <summary>First context byte (prompt-pane state; role not pinned).</summary>
    public byte Arg0 { get; init; }

    /// <summary>Second context byte - the caller-supplied confirm value (role not pinned).</summary>
    public byte Arg1 { get; init; }

    /// <summary>Third context byte (prompt-pane state; role not pinned).</summary>
    public byte Arg2 { get; init; }

    /// <summary>The prompt payload; single-byte length prefix on the wire, so at most 255 bytes.</summary>
    public byte[] Payload { get; init; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Confirm;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Payload.Length > byte.MaxValue)
            throw new InvalidOperationException(
                $"0x31 Confirm: payload length {Payload.Length} exceeds the wire u8 limit ({byte.MaxValue}).");

        writer.WriteByte(Arg0);
        writer.WriteByte(Arg1);
        writer.WriteByte(Arg2);
        writer.WriteByte((byte)Payload.Length);
        writer.WriteBytes(Payload);
    }

    /// <inheritdoc />
    public static ConfirmPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var arg0 = reader.ReadByte();
        var arg1 = reader.ReadByte();
        var arg2 = reader.ReadByte();
        var len = reader.ReadByte();

        return new ConfirmPacket
        {
            Arg0 = arg0,
            Arg1 = arg1,
            Arg2 = arg2,
            Payload = reader.ReadBytes(len).ToArray()
        };
    }
}
