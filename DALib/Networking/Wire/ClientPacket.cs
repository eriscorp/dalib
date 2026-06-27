using System;
using DALib.Networking.Crypto;

namespace DALib.Networking.Wire;

/// <summary>
///     Abstract base record for any client-to-server packet.
/// </summary>
/// <remarks>
///     Provides a place for cross-cutting state and shared default behavior. Concrete
///     packets are sealed records derived from this type (or from a per-opcode abstract
///     record that, in turn, derives from this type for multi-variant opcodes). Encryption
///     method is opcode-derived by the codec via
///     <c>CryptoState.GetClientEncryptMethod</c>; packets do not declare it.
/// </remarks>
public abstract record ClientPacket : IClientPacket
{
    /// <inheritdoc />
    public abstract byte Opcode { get; }

    /// <inheritdoc />
    public virtual EncryptMethod EncryptMethod => CryptoState.GetClientEncryptMethod(Opcode);

    /// <inheritdoc />
    public abstract void WriteBody(IPacketWriter writer);

    /// <inheritdoc />
    public byte[] ToBody()
    {
        var writer = new PacketWriter();
        WriteBody(writer);

        return writer.ToArray();
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ToBodyMemory() => ToBody();
}
