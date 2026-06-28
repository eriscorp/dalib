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
///         <item><description><strong>3 - point:</strong> <c>[u16 BE X][u16 BE Y][u8 Flag]</c>, the
///         clicked tile (used for doors and signposts). The trailing flag is the clicked sprite's
///         vertical-anchor: <c>0</c> = above-tile sprite (door panel, awning), <c>1</c> = floor-aligned
///         sprite (door frame, signpost, ground item). Retail always emits it (send length 7);
///         see comhaigne <c>docs/protocol/client/0x43-point-click.md</c> (USDA RE 2026-04-29).</description></item>
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

    /// <summary>
    ///     Vertical-anchor flag of the clicked sprite; set only when <see cref="ClickType" /> is 3.
    ///     <c>0</c> = above-tile sprite (door panel, awning), <c>1</c> = floor-aligned sprite (door
    ///     frame, signpost, ground item).
    /// </summary>
    public byte? Flag { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Click;

    /// <summary>Builds an entity click carrying the target object's <paramref name="targetId" />.</summary>
    public static ClickPacket Entity(uint targetId) =>
        new() { ClickType = ClickTypeEntity, TargetId = targetId };

    /// <summary>
    ///     Builds a tile click at (<paramref name="x" />, <paramref name="y" />) carrying the clicked
    ///     sprite's anchor <paramref name="flag" /> (0 = above-tile, 1 = floor-aligned).
    /// </summary>
    public static ClickPacket Point(ushort x, ushort y, byte flag) =>
        new() { ClickType = ClickTypePoint, X = x, Y = y, Flag = flag };

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
                if (X is null || Y is null || Flag is null)
                    throw new InvalidOperationException(
                        $"{nameof(ClickPacket)} with {nameof(ClickType)}=3 requires non-null {nameof(X)}, {nameof(Y)}, and {nameof(Flag)}.");
                writer.WriteUInt16(X.Value);
                writer.WriteUInt16(Y.Value);
                writer.WriteByte(Flag.Value);
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

        switch (clickType)
        {
            case ClickTypeEntity:
                return new ClickPacket { ClickType = clickType, TargetId = reader.ReadUInt32() };

            case ClickTypePoint:
                var x = reader.ReadUInt16();
                var y = reader.ReadUInt16();
                // Retail always sends the trailing anchor flag (7-byte send); tolerate its absence.
                byte? flag = reader.Position < reader.Length ? reader.ReadByte() : null;
                return new ClickPacket { ClickType = clickType, X = x, Y = y, Flag = flag };

            default:
                throw new InvalidDataException(
                    $"{nameof(ClickPacket)}: unknown click type 0x{clickType:X2}.");
        }
    }
}
