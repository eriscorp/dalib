using System;
using DALib.Enums;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x05 (S->C) - establishes the receiving character's own appearance on the world server:
///     world-object id plus appearance fields (direction, class, flag byte, gender). This is the
///     self ("this is me") identity packet; use 0x33 DisplayUser for other users.
/// </summary>
/// <remarks>
///     Wire format is 9 bytes flat: <c>[u32 Id][u8 Direction][u8 Unknown1][u8 Class][u8 Flags]
///     [u8 Gender]</c>.
/// </remarks>
[ServerOpcode(ServerOpcode.UserAppearance)]
public sealed record UserAppearancePacket : ServerPacket
{
    /// <summary>
    ///     Flag bit <c>0x80</c>. When set, gates the self-identity cache update. Not set in normal
    ///     use; treat this packet as self-only and emit <see cref="Flags" /> = 0, using 0x33
    ///     DisplayUser for other users.
    /// </summary>
    public const byte FlagDoNotCacheSelfId = 0x80;

    /// <summary>World-object id assigned to the receiving character.</summary>
    public required uint Id { get; set; }

    /// <summary>Cardinal facing direction (0-3).</summary>
    public byte Direction { get; set; }

    /// <summary>Unknown byte. No known consumer; emit 0.</summary>
    public byte Unknown1 { get; set; }

    /// <summary>Character class.</summary>
    public byte Class { get; set; }

    /// <summary>
    ///     Flag byte. Bit <see cref="FlagDoNotCacheSelfId" /> (0x80) gates the self-identity cache
    ///     update; other bits are unused. Emit 0 for normal world entry.
    /// </summary>
    public byte Flags { get; set; }

    /// <summary>Character gender.</summary>
    public Gender Gender { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.UserAppearance;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(Id);
        writer.WriteByte(Direction);
        writer.WriteByte(Unknown1);
        writer.WriteByte(Class);
        writer.WriteByte(Flags);
        writer.WriteByte((byte)Gender);
    }

    /// <inheritdoc />
    public static UserAppearancePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var id = reader.ReadUInt32();
        var direction = reader.ReadByte();
        var unknown1 = reader.ReadByte();
        var @class = reader.ReadByte();
        var flags = reader.ReadByte();
        var gender = (Gender)reader.ReadByte();

        return new UserAppearancePacket
        {
            Id = id,
            Direction = direction,
            Unknown1 = unknown1,
            Class = @class,
            Flags = flags,
            Gender = gender,
        };
    }
}
