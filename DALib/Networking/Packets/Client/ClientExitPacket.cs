using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     The two-phase exit signal carried by <see cref="ClientExitPacket" />.
/// </summary>
public enum ExitSignal : byte
{
    /// <summary>0 - the user confirmed; actually exit now. The server tears the session down.</summary>
    Confirm = 0,

    /// <summary>1 - the exit dialog just opened; please acknowledge.</summary>
    Request = 1
}

/// <summary>
///     0x0B (C->S) - exit-dialog handshake. Carries a single <see cref="ExitSignal" /> byte:
///     <see cref="ExitSignal.Request" /> when the safe-quit dialog opens, and
///     <see cref="ExitSignal.Confirm" /> when the user confirms the exit.
/// </summary>
/// <remarks>
///     On <see cref="ExitSignal.Confirm" /> the server disconnects the session. On
///     <see cref="ExitSignal.Request" /> the server acknowledges (S->C 0x4C).
/// </remarks>
[ClientOpcode(ClientOpcode.ClientExit)]
public sealed record ClientExitPacket : ClientPacket
{
    /// <summary>Which phase of the exit handshake this packet represents.</summary>
    public required ExitSignal Signal { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.ClientExit;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte((byte)Signal);

    /// <inheritdoc />
    public static ClientExitPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new ClientExitPacket
        {
            Signal = (ExitSignal)reader.ReadByte(),
        };
    }
}
