using DALib.Networking.Crypto;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

public class PacketEncryptMethodTests
{
    [Fact]
    public void ClientPacket_EncryptMethod_ResolvesFromClientOpcodeTable()
    {
        new TestNoneClientPacket { Value = 0, Name = "" }.EncryptMethod
            .Should().Be(EncryptMethod.None);   // 0x10
        new TestNormalClientPacket { Name = "", Password = "" }.EncryptMethod
            .Should().Be(EncryptMethod.Normal); // 0x02
        new TestMd5KeyClientPacket { Text = "" }.EncryptMethod
            .Should().Be(EncryptMethod.MD5Key); // 0xFC
    }

    [Fact]
    public void ServerPacket_EncryptMethod_ResolvesFromServerOpcodeTable()
    {
        new TestNoneServerPacket { Tag = 0 }.EncryptMethod
            .Should().Be(EncryptMethod.None);   // 0x40
        new TestNormalServerPacket { Status = 0 }.EncryptMethod
            .Should().Be(EncryptMethod.Normal); // 0x01
        new TestMd5KeyServerPacket { Value = 0 }.EncryptMethod
            .Should().Be(EncryptMethod.MD5Key); // 0xFE
    }

    [Fact]
    public void EncryptMethod_IsDirectionSpecific_ForSameOpcode()
    {
        // Opcode 0x10 is None on the client table but catch-all MD5Key on the server
        // table. The packet's direction (via its base record) picks the right one.
        new TestNoneClientPacket { Value = 0, Name = "" }.EncryptMethod
            .Should().Be(EncryptMethod.None);
        CryptoState.GetServerEncryptMethod(0x10).Should().Be(EncryptMethod.MD5Key);
    }

    [Fact]
    public void EncryptMethod_CanBeOverridden_ForEdgeCases()
    {
        new OverriddenEncryptMethodPacket().EncryptMethod.Should().Be(EncryptMethod.None);
    }

    // A packet whose opcode (0xFC) would normally resolve to MD5Key, but which overrides
    // EncryptMethod - the escape hatch for the edge cases where opcode-derivation is wrong.
    private sealed record OverriddenEncryptMethodPacket : ClientPacket
    {
        public override byte Opcode => 0xFC;
        public override EncryptMethod EncryptMethod => EncryptMethod.None;

        public override void WriteBody(IPacketWriter writer) { }
    }
}
