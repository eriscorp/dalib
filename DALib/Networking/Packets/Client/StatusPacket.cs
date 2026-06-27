using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x79 (C->S) - set the player's social/group status (the "available", "grouped", "trading"
///     etc. indicator). Body: <c>[u8 Status]</c>.
/// </summary>
/// <remarks>
///     <see cref="Status" /> is a small enum value; values 0-7 are accepted and stored as the
///     player's group status, anything higher is ignored.
/// </remarks>
[ClientOpcode(ClientOpcode.Status)]
public sealed record StatusPacket : ClientPacket
{
    /// <summary>The social/group status value (0-7 are accepted by the server).</summary>
    public required byte Status { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Status;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Status);

    /// <inheritdoc />
    public static StatusPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new StatusPacket
        {
            Status = reader.ReadByte(),
        };
    }
}
