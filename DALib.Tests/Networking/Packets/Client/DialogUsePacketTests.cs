using System.Reflection;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x3A DialogUse (C->S) - the multi-variant dialog response. Pins each form's
///     wire layout, verifies stateless tag dispatch, the round-trip through the codec's
///     dialog-obfuscation + MD5Key path, and the consumer-extension story (a
///     <c>[DialogResponseType]</c> variant declared in this test assembly parses strongly-typed
///     when the assembly is passed to the codec).
/// </summary>
public class DialogUsePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // prefix: [01] type [00 00 00 2A] objId BE [FF 00] pursuitId BE [00 03] pursuitIndex BE
    private static readonly byte[] Prefix =
        [0x01, 0x00, 0x00, 0x00, 0x2A, 0xFF, 0x00, 0x00, 0x03];

    private static DialogNavigationPacket Nav() => new()
        { ObjectType = 0x01, ObjectId = 0x2A, PursuitId = 0xFF00, PursuitIndex = 3 };

    [Fact]
    public void Navigation_WriteBody_IsBarePrefix()
    {
        Nav().ToBody().Should().Equal(Prefix);
    }

    [Fact]
    public void Option_WriteBody_PinsLayout()
    {
        var packet = new DialogOptionResponsePacket
            { ObjectType = 0x01, ObjectId = 0x2A, PursuitId = 0xFF00, PursuitIndex = 3, Option = 0x02 };

        // prefix + [01 tag][02 option]
        packet.ToBody().Should().Equal(
            0x01, 0x00, 0x00, 0x00, 0x2A, 0xFF, 0x00, 0x00, 0x03, 0x01, 0x02);
    }

    [Fact]
    public void Text_WriteBody_PinsLayout()
    {
        var packet = new DialogTextResponsePacket
            { ObjectType = 0x01, ObjectId = 0x2A, PursuitId = 0xFF00, PursuitIndex = 3, Text = "hi" };

        // prefix + [02 tag][02 len][68 69 "hi"]
        packet.ToBody().Should().Equal(
            0x01, 0x00, 0x00, 0x00, 0x2A, 0xFF, 0x00, 0x00, 0x03, 0x02, 0x02, 0x68, 0x69);
    }

    [Fact]
    public void Parse_BareBody_IsNavigation()
    {
        DialogUsePacket.Parse(Prefix).Should().BeOfType<DialogNavigationPacket>();
    }

    [Fact]
    public void Parse_Tag01_IsOptionResponse()
    {
        byte[] body = [.. Prefix, 0x01, 0x07];

        var parsed = DialogUsePacket.Parse(body).Should().BeOfType<DialogOptionResponsePacket>().Subject;
        parsed.Option.Should().Be((byte)0x07);
        parsed.PursuitId.Should().Be((ushort)0xFF00);
    }

    [Fact]
    public void Parse_Tag02_IsTextResponse()
    {
        byte[] body = [.. Prefix, 0x02, 0x02, 0x68, 0x69];

        var parsed = DialogUsePacket.Parse(body).Should().BeOfType<DialogTextResponsePacket>().Subject;
        parsed.Text.Should().Be("hi");
    }

    [Fact]
    public void Parse_UnknownTag_Throws()
    {
        byte[] body = [.. Prefix, 0x7F, 0x00];

        var act = () => DialogUsePacket.Parse(body);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ResponseType_ExposesTheTag()
    {
        Nav().ResponseType.Should().BeNull();
        new DialogOptionResponsePacket
            { ObjectType = 1, ObjectId = 1, PursuitId = 1, PursuitIndex = 1, Option = 0 }
            .ResponseType.Should().Be(DialogUsePacket.TagMenu);
        new DialogTextResponsePacket
            { ObjectType = 1, ObjectId = 1, PursuitId = 1, PursuitIndex = 1, Text = "" }
            .ResponseType.Should().Be(DialogUsePacket.TagText);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(DialogUsePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality across all fields + exact type
    }

    public static TheoryData<DialogUsePacket> RoundTripCases() =>
    [
        new DialogNavigationPacket { ObjectType = 0x01, ObjectId = 12345, PursuitId = 0xFF00, PursuitIndex = 2 },
        new DialogOptionResponsePacket { ObjectType = 0x04, ObjectId = 7, PursuitId = 5000, PursuitIndex = 4, Option = 3 },
        new DialogTextResponsePacket { ObjectType = 0x01, ObjectId = 99, PursuitId = 42, PursuitIndex = 1, Text = "Kedian" },
    ];

    // ---- consumer-extension story -------------------------------------------------------------

    // The codec scans the assemblies it is given for [DialogResponseType] variants. A real
    // consumer passes [DALibAssembly, theirAssembly]; here we pass the test assembly alone,
    // because the test harness deliberately mirrors DALib's opcodes (e.g. 0x10) and combining
    // it with DALib would trip the duplicate-opcode guard. The WireInto discovery path exercised
    // is identical either way.
    private static readonly Assembly[] ConsumerAssemblies = [typeof(DialogSliderResponsePacket).Assembly];

    [Fact]
    public void ConsumerVariant_RoundTrips_WhenItsAssemblyIsScanned()
    {
        var codec = new PacketCodec(ConsumerAssemblies);
        var crypto = MakeCrypto();
        var original = new DialogSliderResponsePacket
            { ObjectType = 0x01, ObjectId = 8, PursuitId = 0xFF01, PursuitIndex = 6, Value = 0x1234 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DialogSliderResponsePacket>().Subject;
        typed.Value.Should().Be((ushort)0x1234);
    }

    [Fact]
    public void ConsumerVariant_IsNotKnown_ToTheDefaultCodec()
    {
        // The DALib-only codec's 0x3A table holds only the built-in 0x01/0x02 tags, so a
        // consumer's 0x03 frame throws rather than being mis-parsed.
        var consumerCodec = new PacketCodec(ConsumerAssemblies);
        var defaultCodec = new PacketCodec();
        var crypto = MakeCrypto();
        var wire = consumerCodec.EncodeClient(
            new DialogSliderResponsePacket
                { ObjectType = 1, ObjectId = 1, PursuitId = 1, PursuitIndex = 1, Value = 1 },
            crypto);

        var act = () => defaultCodec.ParseClientPacket(wire, crypto);

        act.Should().Throw<InvalidDataException>();
    }
}

/// <summary>
///     A net-new dialog response form declared <em>outside</em> DALib (here, the test assembly):
///     tag <c>0x03</c>, a u16 payload. Demonstrates that a consumer extends 0x3A with a
///     strongly-typed variant and no DALib edit - exactly the DOOMVAS-extension intent.
/// </summary>
[DialogResponseType(0x03)]
public sealed record DialogSliderResponsePacket : DialogUsePacket
{
    public required ushort Value { get; init; }

    public override byte? ResponseType => 0x03;

    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(0x03);
        writer.WriteUInt16(Value);
    }

    public static DialogSliderResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId, pursuitIndex) = ReadPrefix(ref reader);
        reader.ReadByte(); // tag

        return new DialogSliderResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            PursuitIndex = pursuitIndex,
            Value = reader.ReadUInt16(),
        };
    }
}
