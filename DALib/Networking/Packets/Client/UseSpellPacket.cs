using System;
using System.Buffers.Binary;
using System.Text;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x0F (C->S) - cast a spell from a spellbook slot. Body: <c>[u8 slot][args...]</c>. The bytes
///     after the slot are spell-type-dependent <see cref="Args" /> with no in-packet discriminator;
///     the receiver resolves their shape from the spell occupying <see cref="Slot" />.
/// </summary>
/// <remarks>
///     Three argument shapes are supported:
///     <list type="bullet">
///         <item>
///             <description>
///                 <strong>No-target / self</strong> - <see cref="Args" /> is empty; the body is just
///                 the slot.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Targeted</strong> - <see cref="Args" /> is
///                 <c>[u32 BE targetSerial][u16 BE x][u16 BE y]</c>. The point is the target's own
///                 map position; servers typically read only the serial.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Prompt</strong> - <see cref="Args" /> is the entered text as raw Latin-1
///                 bytes, with <em>no</em> length prefix.
///             </description>
///         </item>
///     </list>
///     Nothing on the wire distinguishes these - there is no length or type field. <see cref="Args" />
///     is written verbatim after the slot; wire framing and trailing C->S padding are the codec's
///     concern. Use the static factories for the known shapes; <see cref="Args" /> is exposed directly
///     for anything else.
/// </remarks>
[ClientOpcode(ClientOpcode.UseSpell)]
public sealed record UseSpellPacket : ClientPacket
{
    /// <summary>Spellbook slot of the spell being cast.</summary>
    public required byte Slot { get; init; }

    /// <summary>
    ///     Spell-type-dependent argument bytes following the slot, written verbatim. Empty for a
    ///     no-target spell; <c>[u32 serial][u16 x][u16 y]</c> for a targeted spell; raw prompt text
    ///     for a prompt spell. See the type remarks for the shapes and why there is no wire
    ///     discriminator.
    /// </summary>
    public byte[] Args { get; init; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.UseSpell;

    /// <summary>Builds a no-target / self cast: just the slot, no arguments.</summary>
    public static UseSpellPacket NoTarget(byte slot) => new() { Slot = slot };

    /// <summary>
    ///     Builds a targeted cast at a creature by serial, with the target point zeroed. The point is
    ///     typically server-ignored (only the serial is read), so zeros are a safe default. Use the
    ///     four-argument overload to carry an explicit point.
    /// </summary>
    public static UseSpellPacket Targeted(byte slot, uint targetSerial) =>
        Targeted(slot, targetSerial, 0, 0);

    /// <summary>
    ///     Builds a targeted cast carrying the target object's serial and map point as
    ///     <c>[u32 serial][u16 x][u16 y]</c> (all big-endian). The point is the target's own map
    ///     position at cast time; servers typically read only the serial.
    /// </summary>
    public static UseSpellPacket Targeted(byte slot, uint targetSerial, ushort x, ushort y)
    {
        var args = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(args, targetSerial);
        BinaryPrimitives.WriteUInt16BigEndian(args.AsSpan(4), x);
        BinaryPrimitives.WriteUInt16BigEndian(args.AsSpan(6), y);

        return new UseSpellPacket { Slot = slot, Args = args };
    }

    /// <summary>
    ///     Builds a prompt cast carrying the entered <paramref name="text" /> as raw Latin-1 bytes
    ///     (no length prefix).
    /// </summary>
    public static UseSpellPacket Prompt(byte slot, string text) =>
        new() { Slot = slot, Args = Encoding.Latin1.GetBytes(text) };

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteBytes(Args);
    }

    /// <inheritdoc />
    public static UseSpellPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var args = reader.Remaining.ToArray();

        return new UseSpellPacket
        {
            Slot = slot,
            Args = args,
        };
    }
}
