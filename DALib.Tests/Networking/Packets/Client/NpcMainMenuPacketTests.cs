using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x39 NpcMainMenu (C->S) - the abstract <see cref="NpcMainMenuPacket" /> base and
///     its send-side form variants. Each form pins its exact <c>WriteBody</c> bytes and round-trips
///     through its own <c>ParseResponse</c>; the bare select form additionally round-trips through
///     the codec (which layers MD5Key encryption over the dialog-obfuscation header). The merchant
///     tail is not self-describing, so the codec auto-dispatches every inbound 0x39 to the bare
///     select form - that silent-drop is pinned too.
/// </summary>
public class NpcMainMenuPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // Shared prefix across every test: [01] type=creature [00 00 AB CD] objectId BE [00 05] pursuitId BE
    private static readonly byte[] Prefix =
        [0x01, 0x00, 0x00, 0xAB, 0xCD, 0x00, 0x05];

    [Fact]
    public void Select_WriteBody_PinsBarePrefix()
    {
        var packet = new NpcMainMenuSelectPacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 0x0000ABCD,
            PursuitId = 0x0005,
        };

        packet.ToBody().Should().Equal(Prefix);
    }

    [Fact]
    public void Text_WriteBody_PinsLayout()
    {
        var packet = new NpcTextResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 0x0000ABCD,
            PursuitId = 0x0005,
            Text = "3",
        };

        // prefix + [01][33]  (len=1, '3')
        packet.ToBody().Should().Equal((byte[]) [.. Prefix, 0x01, 0x33]);
    }

    [Fact]
    public void TextPair_WriteBody_PinsNameThenQuantity()
    {
        var packet = new NpcTextPairResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 0x0000ABCD,
            PursuitId = 0x0005,
            Name = "apple",
            Quantity = "2",
        };

        // prefix + [05][apple][01][32]  - name first, then quantity
        packet.ToBody().Should().Equal((byte[])
            [.. Prefix, 0x05, 0x61, 0x70, 0x70, 0x6C, 0x65, 0x01, 0x32]);
    }

    [Fact]
    public void Option_WriteBody_PinsSingleByte()
    {
        var packet = new NpcOptionResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 0x0000ABCD,
            PursuitId = 0x0005,
            Option = 0x03,
        };

        packet.ToBody().Should().Equal((byte[]) [.. Prefix, 0x03]);
    }

    [Fact]
    public void OptionArgument_WriteBody_PinsWrappedByte()
    {
        var packet = new NpcOptionArgumentResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 0x0000ABCD,
            PursuitId = 0x0005,
            Option = 0x03,
        };

        // prefix + [01][03][01]
        packet.ToBody().Should().Equal((byte[]) [.. Prefix, 0x01, 0x03, 0x01]);
    }

    [Fact]
    public void Handle_WriteBody_PinsLayout()
    {
        var packet = new NpcHandleResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 0x0000ABCD,
            PursuitId = 0x0005,
            Handle = 0xDEADBEEF,
            Param = 0x01,
        };

        // prefix + [01][DE AD BE EF][01]
        packet.ToBody().Should().Equal((byte[])
            [.. Prefix, 0x01, 0xDE, 0xAD, 0xBE, 0xEF, 0x01]);
    }

    [Fact]
    public void Text_ParseResponse_RoundTrips()
    {
        var original = new NpcTextResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeItem,
            ObjectId = 999,
            PursuitId = 42,
            Text = "hello",
        };

        var parsed = NpcTextResponsePacket.ParseResponse(original.ToBody());

        parsed.Should().Be(original);
    }

    [Fact]
    public void TextPair_ParseResponse_RoundTrips()
    {
        var original = new NpcTextPairResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 12345,
            PursuitId = 7,
            Name = "stick",
            Quantity = "10",
        };

        var parsed = NpcTextPairResponsePacket.ParseResponse(original.ToBody());

        parsed.Should().Be(original);
    }

    [Fact]
    public void OptionArgument_ParseResponse_RoundTrips()
    {
        var original = new NpcOptionArgumentResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 12345,
            PursuitId = 78,
            Option = 0x09,
        };

        var parsed = NpcOptionArgumentResponsePacket.ParseResponse(original.ToBody());

        parsed.Should().Be(original);
    }

    [Fact]
    public void Handle_ParseResponse_RoundTrips()
    {
        var original = new NpcHandleResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 12345,
            PursuitId = 75,
            Handle = 0x01020304,
            Param = 0x01,
        };

        var parsed = NpcHandleResponsePacket.ParseResponse(original.ToBody());

        parsed.Should().Be(original);
    }

    [Fact]
    public void Select_RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new NpcMainMenuSelectPacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 12345,
            PursuitId = 7,
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<NpcMainMenuSelectPacket>().Subject;
        typed.ObjectType.Should().Be(NpcMainMenuPacket.ObjectTypeCreature);
        typed.ObjectId.Should().Be(12345u);
        typed.PursuitId.Should().Be((ushort)7);
    }

    [Fact]
    public void Codec_DispatchesTailedFrameToBareSelect_DroppingTail()
    {
        // The merchant tail is not self-describing, so the codec resolves every inbound 0x39 to the
        // bare select form: the prefix survives, the tail is silently dropped. A consumer that knows
        // the form re-parses the body with the matching variant's ParseResponse.
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var sent = new NpcTextResponsePacket
        {
            ObjectType = NpcMainMenuPacket.ObjectTypeCreature,
            ObjectId = 12345,
            PursuitId = 7,
            Text = "dropped",
        };

        var wire = codec.EncodeClient(sent, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<NpcMainMenuSelectPacket>().Subject;
        typed.ObjectId.Should().Be(12345u);
        typed.PursuitId.Should().Be((ushort)7);
    }
}
