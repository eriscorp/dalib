using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x6A (C->S) - drive a mini-game. The body opens with a <see cref="MiniGameActionType" /> action byte
///     that selects the form and any tail. The concrete forms are the sealed records deriving from this base
///     (<see cref="OpenMiniGamePacket" />, <see cref="SubmitMiniGamePacket" />,
///     <see cref="SyncMiniGamePacket" />, <see cref="ResultMiniGamePacket" />).
/// </summary>
/// <remarks>
///     Binary-verified send (the retail client has full <c>SMiniGame</c> / <c>MiniGameDialog</c> RTTI); not
///     emitted by Hybrasyl or Chaos. Modeled for wire completeness: the structure of each observed form is
///     pinned, but the field semantics are inferred.
/// </remarks>
[ClientOpcode(ClientOpcode.MiniGame)]
public abstract record MiniGamePacket : ClientPacket
{
    /// <summary>The action byte that selects this variant's form.</summary>
    public abstract MiniGameActionType ActionType { get; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.MiniGame;

    /// <summary>Writes the leading <c>[u8 action]</c>. Variants call this, then append their tail.</summary>
    protected void WritePrefix(IPacketWriter writer) => writer.WriteByte((byte)ActionType);

    /// <summary>
    ///     Parses a 0x6A body, dispatching on the leading action byte to the matching variant. This is the
    ///     standalone entry and what <see cref="ClientOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static MiniGamePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (MiniGameActionType)reader.ReadByte();

        switch (type)
        {
            case MiniGameActionType.Open:
                return new OpenMiniGamePacket();
            case MiniGameActionType.Submit:
            {
                var id = reader.ReadByte();
                var len = reader.ReadByte();

                return new SubmitMiniGamePacket { Id = id, Data = reader.ReadBytes(len).ToArray() };
            }
            case MiniGameActionType.Sync:
                return new SyncMiniGamePacket { Sequence = reader.ReadUInt32() };
            case MiniGameActionType.Result:
            {
                reader.ReadByte(); // constant 0x02
                var flag = reader.ReadByte();
                reader.ReadByte(); // constant 0x00

                return new ResultMiniGamePacket { Flag = flag };
            }
            default:
                throw new InvalidDataException(
                    $"0x6A MiniGame: unknown action 0x{(byte)type:X2}.");
        }
    }
}

/// <summary>
///     0x6A action 5 - open/enter the mini-game. Prefix only.
/// </summary>
public sealed record OpenMiniGamePacket : MiniGamePacket
{
    /// <inheritdoc />
    public override MiniGameActionType ActionType => MiniGameActionType.Open;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}

/// <summary>
///     0x6A action 6 - submit a data blob. Tail <c>[u8 Id][u8 len][bytes Data]</c>.
/// </summary>
public sealed record SubmitMiniGamePacket : MiniGamePacket
{
    /// <summary>A leading selector/id byte (role inferred).</summary>
    public required byte Id { get; init; }

    /// <summary>The submitted payload; single-byte length prefix on the wire, so at most 255 bytes.</summary>
    public byte[] Data { get; init; } = [];

    /// <inheritdoc />
    public override MiniGameActionType ActionType => MiniGameActionType.Submit;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Data.Length > byte.MaxValue)
            throw new InvalidOperationException(
                $"0x6A MiniGame Submit: data length {Data.Length} exceeds the wire u8 limit ({byte.MaxValue}).");

        WritePrefix(writer);
        writer.WriteByte(Id);
        writer.WriteByte((byte)Data.Length);
        writer.WriteBytes(Data);
    }
}

/// <summary>
///     0x6A action 7 - sync with an incrementing sequence counter. Tail <c>[u32 BE Sequence]</c>.
/// </summary>
public sealed record SyncMiniGamePacket : MiniGamePacket
{
    /// <summary>The client's per-send incrementing sequence counter.</summary>
    public required uint Sequence { get; init; }

    /// <inheritdoc />
    public override MiniGameActionType ActionType => MiniGameActionType.Sync;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteUInt32(Sequence);
    }
}

/// <summary>
///     0x6A action 8 - a result/state sub-action. Tail <c>[u8 0x02][u8 Flag][u8 0x00]</c>; the client wraps
///     a single <see cref="Flag" /> byte between two constants.
/// </summary>
public sealed record ResultMiniGamePacket : MiniGamePacket
{
    /// <summary>The result/state flag (observed as 0 or 1; role inferred).</summary>
    public required byte Flag { get; init; }

    /// <inheritdoc />
    public override MiniGameActionType ActionType => MiniGameActionType.Result;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(0x02); // constant
        writer.WriteByte(Flag);
        writer.WriteByte(0x00); // constant
    }
}
