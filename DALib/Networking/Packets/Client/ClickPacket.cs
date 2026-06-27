using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x43 (C->S) - click on the game world: either an entity (by serial) or a map tile (by X,Y).
///     The body is <c>[u8 ClickType]</c> followed by a type-dependent payload.
/// </summary>
/// <remarks>
///     The leading byte is a click-type discriminator:
///     <list type="bullet">
///         <item><description><strong>1 - entity:</strong> <c>[u32 BE TargetId]</c>, the clicked
///         object's serial.</description></item>
///         <item><description><strong>3 - point:</strong> <c>[u16 BE X][u16 BE Y]</c>, the clicked
///         tile (used for doors and signposts).</description></item>
///     </list>
///     Use the static factories.
/// </remarks>
[ClientOpcode(ClientOpcode.Click)]
public sealed record ClickPacket : ClientPacket
{
    /// <summary>Discriminator for an entity click (a u32 target id follows).</summary>
    public const byte ClickTypeEntity = 1;

    /// <summary>Discriminator for a tile click (two u16 coordinates follow).</summary>
    public const byte ClickTypePoint = 3;

    /// <summary>The click-type discriminator: 1 entity, 3 point.</summary>
    public required byte ClickType { get; init; }

    /// <summary>The clicked entity's serial; set only when <see cref="ClickType" /> is 1.</summary>
    public uint? TargetId { get; init; }

    /// <summary>X of the clicked tile; set only when <see cref="ClickType" /> is 3.</summary>
    public ushort? X { get; init; }

    /// <summary>Y of the clicked tile; set only when <see cref="ClickType" /> is 3.</summary>
    public ushort? Y { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Click;

    /// <summary>Builds an entity click carrying the target object's <paramref name="targetId" />.</summary>
    public static ClickPacket Entity(uint targetId) =>
        new() { ClickType = ClickTypeEntity, TargetId = targetId };

    /// <summary>Builds a tile click at (<paramref name="x" />, <paramref name="y" />).</summary>
    public static ClickPacket Point(ushort x, ushort y) =>
        new() { ClickType = ClickTypePoint, X = x, Y = y };

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(ClickType);

        switch (ClickType)
        {
            case ClickTypeEntity:
                if (TargetId is null)
                    throw new InvalidOperationException(
                        $"{nameof(ClickPacket)} with {nameof(ClickType)}=1 requires a non-null {nameof(TargetId)}.");
                writer.WriteUInt32(TargetId.Value);
                break;

            case ClickTypePoint:
                if (X is null || Y is null)
                    throw new InvalidOperationException(
                        $"{nameof(ClickPacket)} with {nameof(ClickType)}=3 requires non-null {nameof(X)} and {nameof(Y)}.");
                writer.WriteUInt16(X.Value);
                writer.WriteUInt16(Y.Value);
                break;

            default:
                throw new InvalidOperationException(
                    $"{nameof(ClickPacket)} cannot serialize unknown {nameof(ClickType)} 0x{ClickType:X2}.");
        }
    }

    /// <inheritdoc />
    public static ClickPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var clickType = reader.ReadByte();

        return clickType switch
        {
            ClickTypeEntity => new ClickPacket { ClickType = clickType, TargetId = reader.ReadUInt32() },
            ClickTypePoint => new ClickPacket { ClickType = clickType, X = reader.ReadUInt16(), Y = reader.ReadUInt16() },
            _ => throw new InvalidDataException(
                $"{nameof(ClickPacket)}: unknown click type 0x{clickType:X2}."),
        };
    }
}
