using System;
using System.Collections.Generic;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x07 (S->C) - adds one or more world objects (items, gold piles, creatures,
///     NPCs, other players) to the receiving visible scene. Carries a u16 count
///     followed by that many variant-shaped per-object records.
/// </summary>
/// <remarks>
///     Any object count is supported; the count is a genuine loop count, not a flag.
///     Per-object shape depends on <see cref="WorldObject.Sprite" /> range:
///     <see cref="CreatureWorldObject" /> for <c>[0x4000, 0x8000)</c>,
///     <see cref="ItemWorldObject" /> otherwise. See <see cref="WorldObject" />
///     for the dispatch and rendering semantics.
/// </remarks>
[ServerOpcode(ServerOpcode.DrawObjects)]
public sealed record DrawObjectsPacket : ServerPacket
{
    private const ushort CreatureRangeStart = 0x4000;
    private const ushort CreatureRangeEnd = 0x8000;

    /// <summary>The objects to add. Wire-emits a u16 count followed by each variant body.</summary>
    public IList<WorldObject> Objects { get; set; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.DrawObjects;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Objects.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"DrawObjects: object count {Objects.Count} exceeds wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16((ushort)Objects.Count);

        foreach (var obj in Objects)
            obj.WriteBody(writer);
    }

    /// <inheritdoc />
    public static DrawObjectsPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var count = reader.ReadUInt16();
        var objects = new List<WorldObject>(count);

        for (var i = 0; i < count; i++)
            objects.Add(ParseObject(ref reader));

        return new DrawObjectsPacket { Objects = objects };
    }

    private static WorldObject ParseObject(ref PacketReader reader)
    {
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        var id = reader.ReadUInt32();
        var sprite = reader.ReadUInt16();

        var isCreature = sprite >= CreatureRangeStart && sprite < CreatureRangeEnd;

        if (isCreature)
        {
            var slot0 = reader.ReadByte();
            var slot1 = reader.ReadByte();
            var slot2 = reader.ReadByte();
            var slot3 = reader.ReadByte();
            var direction = reader.ReadByte();
            var unknown = reader.ReadByte();
            var type = reader.ReadByte();
            var name = type == CreatureWorldObject.TypeNamed
                ? reader.ReadString8()
                : string.Empty;

            return new CreatureWorldObject
            {
                X = x,
                Y = y,
                Id = id,
                Sprite = sprite,
                Slot0 = slot0,
                Slot1 = slot1,
                Slot2 = slot2,
                Slot3 = slot3,
                Direction = direction,
                Unknown = unknown,
                Type = type,
                Name = name,
            };
        }
        else
        {
            var color = reader.ReadByte();
            var direction = reader.ReadByte();
            var unknown = reader.ReadByte();

            return new ItemWorldObject
            {
                X = x,
                Y = y,
                Id = id,
                Sprite = sprite,
                Color = color,
                Direction = direction,
                Unknown = unknown,
            };
        }
    }
}

