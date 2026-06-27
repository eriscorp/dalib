using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x64 (S->C) - a leading <c>[u8 Type]</c> selects the form: types 3, 4 and 8 carry a single
///     <c>[u8 Value]</c>; type 7 carries <c>[u32 First][u32 Second]</c> (big-endian); any other type is
///     bare. Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
/// <remarks>
///     Modeled as a single conditional record: <see cref="Value" /> is present for the byte forms;
///     <see cref="First" /> / <see cref="Second" /> are present for the pair form.
/// </remarks>
[ServerOpcode(ServerOpcode.MiniGame)]
public sealed record MiniGamePacket : ServerPacket
{
    /// <summary>The type values whose form carries a single trailing byte (<see cref="Value" />).</summary>
    public static readonly byte[] ByteFormTypes = [0x03, 0x04, 0x08];

    /// <summary>The type value whose form carries two big-endian <c>u32</c>s
    ///     (<see cref="First" />/<see cref="Second" />).</summary>
    public const byte PairFormType = 0x07;

    /// <summary>The leading discriminator byte selecting the form.</summary>
    public required byte Type { get; init; }

    /// <summary>The single trailing byte present for the byte forms (<see cref="Type" /> in {3, 4, 8});
    ///     <see langword="null" /> otherwise. Required to be non-null for those types.</summary>
    public byte? Value { get; init; }

    /// <summary>The first big-endian <c>u32</c> present for the pair form (<see cref="Type" /> == 7);
    ///     <see langword="null" /> otherwise. Required to be non-null for type 7.</summary>
    public uint? First { get; init; }

    /// <summary>The second big-endian <c>u32</c> present for the pair form (<see cref="Type" /> == 7);
    ///     <see langword="null" /> otherwise. Required to be non-null for type 7.</summary>
    public uint? Second { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.MiniGame;

    private static bool IsByteForm(byte type) => Array.IndexOf(ByteFormTypes, type) >= 0;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Type);

        if (IsByteForm(Type))
        {
            if (Value is null)
                throw new InvalidOperationException(
                    $"MiniGamePacket: Value is required when Type is a byte form (3/4/8); Type = {Type}.");

            writer.WriteByte(Value.Value);
        }
        else if (Type == PairFormType)
        {
            if (First is null || Second is null)
                throw new InvalidOperationException(
                    "MiniGamePacket: First and Second are required when Type == 7 (the pair form).");

            writer.WriteUInt32(First.Value);
            writer.WriteUInt32(Second.Value);
        }
    }

    /// <inheritdoc />
    public static MiniGamePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = reader.ReadByte();

        if (IsByteForm(type))
            return new MiniGamePacket { Type = type, Value = reader.ReadByte() };

        if (type == PairFormType)
            return new MiniGamePacket
            {
                Type = type,
                First = reader.ReadUInt32(),
                Second = reader.ReadUInt32(),
            };

        return new MiniGamePacket { Type = type };
    }
}
