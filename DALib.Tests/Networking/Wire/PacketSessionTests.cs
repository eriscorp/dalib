using DALib.Networking.Crypto;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

/// <summary>
///     <see cref="PacketSession" /> is a thin pairing of a shared <see cref="PacketCodec" />
///     with one connection's <see cref="CryptoState" />. These tests verify that every
///     method forwards to the codec carrying that crypto state - round-trips through the
///     session, that the wrapped state is shared (not copied), and the record's value
///     semantics. The exhaustive wire-format coverage lives in <see cref="PacketCodecTests" />.
/// </summary>
public class PacketSessionTests
{
    private static readonly System.Reflection.Assembly[] TestPacketAssemblies =
        [typeof(TestNoneClientPacket).Assembly];

    private static CryptoState MakeReadyCrypto()
    {
        var state = new CryptoState
        {
            EncryptionSeed = 5,
            EncryptionKey = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 },
        };
        state.GenerateKeyTable("Aisling");

        return state;
    }

    private static PacketSession MakeSession(out PacketCodec codec, out CryptoState crypto)
    {
        codec = new PacketCodec(TestPacketAssemblies);
        crypto = MakeReadyCrypto();

        return new PacketSession(codec, crypto);
    }

    // -- Round-trips through the session (forwarding carries Crypto) -----------

    [Fact]
    public void RoundTrip_NoneClientPacket()
    {
        var session = MakeSession(out _, out _);
        var original = new TestNoneClientPacket { Value = 0x77, Name = "Aisling" };

        var parsed = session.ParseClientPacket(session.EncodeClient(original));

        var typed = parsed.Should().BeOfType<TestNoneClientPacket>().Subject;
        typed.Value.Should().Be(0x77);
        typed.Name.Should().Be("Aisling");
    }

    [Fact]
    public void RoundTrip_NoneServerPacket()
    {
        var session = MakeSession(out _, out _);
        var original = new TestNoneServerPacket { Tag = 0xBEEF };

        var parsed = session.ParseServerPacket(session.EncodeServer(original));

        parsed.Should().BeOfType<TestNoneServerPacket>().Subject.Tag.Should().Be(0xBEEF);
    }

    [Fact]
    public void RoundTrip_NormalClientPacket()
    {
        var session = MakeSession(out _, out _);
        var original = new TestNormalClientPacket { Name = "Aisling", Password = "secret" };

        var parsed = session.ParseClientPacket(session.EncodeClient(original));

        var typed = parsed.Should().BeOfType<TestNormalClientPacket>().Subject;
        typed.Name.Should().Be("Aisling");
        typed.Password.Should().Be("secret");
    }

    [Fact]
    public void RoundTrip_Md5KeyClientPacket()
    {
        var session = MakeSession(out _, out _);
        var original = new TestMd5KeyClientPacket { Text = "hello world" };

        var parsed = session.ParseClientPacket(session.EncodeClient(original));

        parsed.Should().BeOfType<TestMd5KeyClientPacket>().Subject.Text.Should().Be("hello world");
    }

    [Fact]
    public void RoundTrip_Md5KeyServerPacket()
    {
        var session = MakeSession(out _, out _);
        var original = new TestMd5KeyServerPacket { Value = 0xDEADBEEF };

        var parsed = session.ParseServerPacket(session.EncodeServer(original));

        parsed.Should().BeOfType<TestMd5KeyServerPacket>().Subject.Value.Should().Be(0xDEADBEEFu);
    }

    [Fact]
    public void RoundTrip_DialogClientPacket_AppliesAndRemovesObfuscation()
    {
        var session = MakeSession(out _, out _);
        var original = new TestDialogClientPacket
        {
            ObjectType = 0x01,
            ObjectId = 0x0000002A,
            PursuitId = 0x0005,
        };

        var parsed = session.ParseClientPacket(session.EncodeClient(original));

        var typed = parsed.Should().BeOfType<TestDialogClientPacket>().Subject;
        typed.ObjectType.Should().Be(0x01);
        typed.ObjectId.Should().Be(0x0000002Au);
        typed.PursuitId.Should().Be((ushort)0x0005);
    }

    // -- Crypto state is shared, not copied -----------------------------------

    [Fact]
    public void EncodeClient_AdvancesTheWrappedCryptoOrdinal()
    {
        var session = MakeSession(out _, out var crypto);
        crypto.ClientOrdinal = 5;

        session.EncodeClient(new TestMd5KeyClientPacket { Text = "x" });

        crypto.ClientOrdinal.Should().Be((byte)6);
        session.Crypto.Should().BeSameAs(crypto);
    }

    [Fact]
    public void EncodeClient_WithExplicitSequence_DoesNotAdvanceTheWrappedCryptoOrdinal()
    {
        var session = MakeSession(out _, out var crypto);
        crypto.ClientOrdinal = 5;

        session.EncodeClient(new TestMd5KeyClientPacket { Text = "x" }, sequence: 0x42);

        crypto.ClientOrdinal.Should().Be((byte)5);
    }

    // -- Slop and None-mode byte equivalence to the codec ---------------------

    [Fact]
    public void EncodeClient_NonePacket_WithSlop_ForwardsSlopToTheCodec()
    {
        var session = MakeSession(out _, out _);
        var packet = new TestNoneClientPacket { Value = 0x01, Name = "" };
        var slop = new byte[] { 0xCA, 0xFE };

        var wire = session.EncodeClient(packet, slop: slop).ToArray();

        // [0xAA] [len] [0x10] [0x01] [string8 ""] [slop CA FE] [trailing 0x00]
        wire.Should().Equal(
            (byte)0xAA,
            (byte)0x00, (byte)0x06,
            (byte)0x10,
            (byte)0x01,
            (byte)0x00,
            (byte)0xCA, (byte)0xFE,
            (byte)0x00);
    }

    [Fact]
    public void EncodeClient_NonePacket_MatchesCodecEncodeWithSameCrypto()
    {
        // None mode is deterministic (no per-packet bRand/sRand), so the session's encode
        // must be byte-identical to calling the codec directly with an equivalent crypto.
        var session = MakeSession(out var codec, out _);
        var standalone = MakeReadyCrypto();
        var packet = new TestNoneClientPacket { Value = 0x33, Name = "eq" };

        var viaSession = session.EncodeClient(packet).ToArray();
        var viaCodec = codec.EncodeClient(packet, standalone).ToArray();

        viaSession.Should().Equal(viaCodec);
    }

    // -- TryGet forwarding ----------------------------------------------------

    [Fact]
    public void TryGetClientPacket_OnCompleteFrame_ShouldYieldPacketAndConsumeIt()
    {
        var session = MakeSession(out _, out _);
        var wire = session.EncodeClient(new TestNoneClientPacket { Value = 0x09, Name = "ok" });

        session.TryGetClientPacket(wire, out var packet, out var consumed).Should().BeTrue();
        packet.Should().BeOfType<TestNoneClientPacket>().Subject.Name.Should().Be("ok");
        consumed.Should().Be(wire.Length);
    }

    [Fact]
    public void TryGetServerPacket_OnShortBuffer_ShouldReturnFalse()
    {
        var session = MakeSession(out _, out _);

        session.TryGetServerPacket(new byte[] { 0xAA }, out var packet, out var consumed)
               .Should().BeFalse();
        packet.Should().BeNull();
        consumed.Should().Be(0);
    }

    // -- Construction validation (fail early) ---------------------------------

    [Fact]
    public void Construction_WithNullCodec_ShouldThrow()
    {
        var act = () => new PacketSession(null!, MakeReadyCrypto());

        act.Should().Throw<ArgumentNullException>().WithParameterName("codec");
    }

    [Fact]
    public void Construction_WithNullCrypto_ShouldThrow()
    {
        var codec = new PacketCodec(TestPacketAssemblies);

        var act = () => new PacketSession(codec, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("crypto");
    }

    // -- Record value semantics -----------------------------------------------

    [Fact]
    public void Equality_IsByWrappedCodecAndCryptoInstances()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();

        var a = new PacketSession(codec, crypto);
        var b = new PacketSession(codec, crypto);
        var differentCrypto = new PacketSession(codec, MakeReadyCrypto());

        a.Should().Be(b);
        a.Should().NotBe(differentCrypto);
    }
}
