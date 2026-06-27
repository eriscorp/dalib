using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x6D (S->C) - a single name string. The body is one <c>[string8]</c> (a
///     <c>[u8 len][len bytes]</c> length-prefixed string). Modeled for protocol completeness;
///     not emitted by typical servers.
/// </summary>
[ServerOpcode(ServerOpcode.FamilyName)]
public sealed record FamilyNamePacket : ServerPacket
{
    /// <summary>The name string (a <c>string8</c>).</summary>
    public required string Name { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.FamilyName;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteString8(Name);

    /// <inheritdoc />
    public static FamilyNamePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new FamilyNamePacket
        {
            Name = reader.ReadString8(),
        };
    }
}
