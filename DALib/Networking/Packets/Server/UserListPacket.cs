using System;
using System.Collections.Generic;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x36 (S->C) - the online-user ("who's online") list, sent in answer to C->S 0x18
///     RequestWorldList. Carries one <see cref="UserListEntry" /> per visible player: class, a
///     relationship-color byte, social status, title, master flag, and name.
/// </summary>
/// <remarks>
///     <para>
///         Body: <c>[u16 totalAllShards][u16 shardCount]</c> then, for each listed user,
///         <c>[u8 Class][u8 Color][u8 SocialStatus][string8 Title][bool IsMaster][string8 Name]</c>.
///         The first count is the total online across all server shards (e.g. Temuair, Medenia); the
///         second is the count on the current shard and equals the rows that follow. <see cref="Parse" />
///         uses the second.
///     </para>
///     <para>
///         <see cref="UserListEntry.Color" /> is a server-computed relationship indicator, not a
///         palette index (a server may use distinct values for guild-mates, near-level players, and
///         others). Modeled as a raw byte because the value is a server policy choice.
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.UserList)]
public sealed record UserListPacket : ServerPacket
{
    /// <summary>
    ///     The listed players. Mutable so a server can accumulate entries while applying per-viewer
    ///     ordering or filtering. The wire count is a u16, so at most 65535 entries may be written.
    /// </summary>
    public IList<UserListEntry> Users { get; set; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.UserList;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Users.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"UserList: user count {Users.Count} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16((ushort)Users.Count);
        writer.WriteUInt16((ushort)Users.Count);

        foreach (var user in Users)
        {
            writer.WriteByte(user.Class);
            writer.WriteByte(user.Color);
            writer.WriteByte((byte)user.SocialStatus);
            writer.WriteString8(user.Title);
            writer.WriteBoolean(user.IsMaster);
            writer.WriteString8(user.Name);
        }
    }

    /// <inheritdoc />
    public static UserListPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        // Two counts: the first is the total online across all server shards (e.g. Temuair, Medenia),
        // the second is the count on the current shard, which equals the rows that follow. Use the second.
        _ = reader.ReadUInt16();
        var count = reader.ReadUInt16();

        // Minimum entry width: class + color + status + master flag + two empty string8 length prefixes.
        const int MinEntrySize = 6;

        var users = new List<UserListEntry>(count);
        for (var i = 0; i < count; i++)
        {
            // Defensive: never overrun if count and body disagree (the DOOMVAS inner-pad also leaves a
            // short tail). Stop when the remaining body can't hold another entry.
            if (reader.Length - reader.Position < MinEntrySize)
                break;

            users.Add(new UserListEntry(
                Class: reader.ReadByte(),
                Color: reader.ReadByte(),
                SocialStatus: (SocialStatus)reader.ReadByte(),
                Title: reader.ReadString8(),
                IsMaster: reader.ReadBoolean(),
                Name: reader.ReadString8()));
        }

        return new UserListPacket { Users = users };
    }
}

/// <summary>
///     One row of a <see cref="UserListPacket" />: a listed player's class, relationship-color byte,
///     social status, title, master flag, and name.
/// </summary>
/// <param name="Class">The player's character class byte.</param>
/// <param name="Color">
///     A server-computed relationship-color indicator. Not a palette index - see
///     <see cref="UserListPacket" />'s remarks.
/// </param>
/// <param name="SocialStatus">The player's social / grouping-availability status indicator.</param>
/// <param name="Title">The player's displayed title (may be empty).</param>
/// <param name="IsMaster">Whether the player is a master (rendered distinctly in the list).</param>
/// <param name="Name">The player's name.</param>
public readonly record struct UserListEntry(
    byte Class,
    byte Color,
    SocialStatus SocialStatus,
    string Title,
    bool IsMaster,
    string Name);
