using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x0D Ignore (C->S) - the IgnoreType discriminator and the conditional
///     [string8 TargetName] that follows only for AddUser / RemoveUser.
/// </summary>
public class IgnorePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void Request_WriteBody_IsSingleTypeByte_NoName()
    {
        IgnorePacket.Request().ToBody().Should().Equal((byte)0x01);
    }

    [Fact]
    public void AddUser_WriteBody_PinsTypeThenName()
    {
        var packet = IgnorePacket.AddUser("Bob");

        // [02 AddUser][03 nameLen][B o b]
        packet.ToBody().Should().Equal(
            (byte)0x02,
            (byte)0x03, (byte)'B', (byte)'o', (byte)'b');
    }

    [Fact]
    public void RemoveUser_WriteBody_PinsTypeThenName()
    {
        var packet = IgnorePacket.RemoveUser("Eve");

        // [03 RemoveUser][03 nameLen][E v e]
        packet.ToBody().Should().Equal(
            (byte)0x03,
            (byte)0x03, (byte)'E', (byte)'v', (byte)'e');
    }

    [Fact]
    public void RoundTrip_Request_PreservesType_NoName()
    {
        var parsed = IgnorePacket.Parse(IgnorePacket.Request().ToBody());

        parsed.IgnoreType.Should().Be(IgnoreType.Request);
        parsed.TargetName.Should().BeNull();
    }

    [Theory]
    [InlineData("Alice")]
    [InlineData("X")]
    public void RoundTrip_AddUser_PreservesName(string name)
    {
        var parsed = IgnorePacket.Parse(IgnorePacket.AddUser(name).ToBody());

        parsed.IgnoreType.Should().Be(IgnoreType.AddUser);
        parsed.TargetName.Should().Be(name);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAddUser()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeClient(IgnorePacket.RemoveUser("Mallory"), crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<IgnorePacket>().Subject;
        typed.IgnoreType.Should().Be(IgnoreType.RemoveUser);
        typed.TargetName.Should().Be("Mallory");
    }

    [Fact]
    public void WriteBody_AddUserWithNullName_Throws()
    {
        var packet = new IgnorePacket { IgnoreType = IgnoreType.AddUser, TargetName = null };

        var act = () => packet.ToBody();

        act.Should().Throw<System.InvalidOperationException>();
    }
}
