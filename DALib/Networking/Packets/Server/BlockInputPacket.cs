using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x51 (S->C) - lock or release player input. Body: <c>[u8 State]</c>, plus a single trailing
///     <c>[u8]</c> only when <see cref="State" /> is 0. State 1 blocks input, 0 releases; the trailing
///     byte on the release form is parsed but otherwise unused. Modeled for protocol completeness;
///     not emitted by typical servers.
/// </summary>
[ServerOpcode(ServerOpcode.BlockInput)]
public sealed record BlockInputPacket : ServerPacket
{
    /// <summary>
    ///     The toggle byte: 1 blocks input, 0 releases. Other values have no effect. Stored as the
    ///     raw byte to preserve it on round-trip.
    /// </summary>
    public required byte State { get; init; }

    /// <summary>
    ///     Trailing byte present only on the release form (<see cref="State" /> is 0); <see langword="null" />
    ///     otherwise. Preserved verbatim for round-tripping. Required to be non-null when
    ///     <see cref="State" /> is 0.
    /// </summary>
    public byte? ReleaseArgument { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.BlockInput;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(State);

        if (State != 0)
            return;

        if (ReleaseArgument is null)
            throw new InvalidOperationException(
                "BlockInputPacket: ReleaseArgument is required when State == 0 (the release form carries a trailing byte).");

        writer.WriteByte(ReleaseArgument.Value);
    }

    /// <inheritdoc />
    public static BlockInputPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var state = reader.ReadByte();

        return new BlockInputPacket
        {
            State = state,
            ReleaseArgument = state == 0 ? reader.ReadByte() : null
        };
    }
}
