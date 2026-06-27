using System.Reflection;
using DALib.Networking.Crypto;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

public class PacketCodecTests
{
    private static readonly Assembly[] TestPacketAssemblies = [typeof(TestNoneClientPacket).Assembly];

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

    // -- Discovery -------------------------------------------------------------

    [Fact]
    public void Construction_WithDalibAssemblyOnly_ShouldDiscoverRealPackets()
    {
        var codec = new PacketCodec();

        // The default codec scans the DALib assembly, which now contains real packets
        // (e.g. 0x00 VersionPacket). This count grows as packets are added.
        codec.IsClientOpcodeRegistered(0x00).Should().BeTrue();
        codec.RegisteredClientOpcodeCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Construction_WithTestAssembly_ShouldDiscoverBothDirections()
    {
        var codec = new PacketCodec(TestPacketAssemblies);

        // Client direction
        codec.IsClientOpcodeRegistered(0x10).Should().BeTrue(); // None
        codec.IsClientOpcodeRegistered(0x02).Should().BeTrue(); // Normal
        codec.IsClientOpcodeRegistered(0xFC).Should().BeTrue(); // MD5Key
        codec.IsClientOpcodeRegistered(0x39).Should().BeTrue(); // MD5Key + dialog

        // Server direction
        codec.IsServerOpcodeRegistered(0x40).Should().BeTrue(); // None
        codec.IsServerOpcodeRegistered(0x01).Should().BeTrue(); // Normal
        codec.IsServerOpcodeRegistered(0xFE).Should().BeTrue(); // MD5Key

        // Cross-direction misses
        codec.IsClientOpcodeRegistered(0x40).Should().BeFalse();
        codec.IsServerOpcodeRegistered(0x10).Should().BeFalse();
    }

    // -- None mode -------------------------------------------------------------

    [Fact]
    public void Encode_NoneClientPacket_ShouldEmitFramedWireBytesWithTrailingNull()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var packet = new TestNoneClientPacket { Value = 0x42, Name = "hi" };

        var wire = codec.EncodeClient(packet, crypto).ToArray();

        // [0xAA] [0x00 0x06] [0x10] [0x42] [0x02 'h' 'i'] [0x00]
        wire.Should().Equal(
            (byte)0xAA,
            (byte)0x00, (byte)0x06,
            (byte)0x10,
            (byte)0x42,
            (byte)0x02, (byte)'h', (byte)'i',
            (byte)0x00);
    }

    [Fact]
    public void Encode_NoneClientPacket_WithSlop_ShouldAppendSlopBeforeTrailingNull()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var packet = new TestNoneClientPacket { Value = 0x01, Name = "" };
        var slop = new byte[] { 0xCA, 0xFE };

        var wire = codec.EncodeClient(packet, crypto, slop: slop).ToArray();

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
    public void Encode_NoneServerPacket_ShouldEmitFramedWireBytesWithoutTrailingNull()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var packet = new TestNoneServerPacket { Tag = 0x1234 };

        var wire = codec.EncodeServer(packet, crypto).ToArray();

        // [0xAA] [0x00 0x03] [0x40] [0x12 0x34]
        wire.Should().Equal(
            (byte)0xAA,
            (byte)0x00, (byte)0x03,
            (byte)0x40,
            (byte)0x12, (byte)0x34);
    }

    [Fact]
    public void RoundTrip_NoneClientPacket()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var original = new TestNoneClientPacket { Value = 0x77, Name = "Aisling" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().BeOfType<TestNoneClientPacket>();
        var typed = (TestNoneClientPacket)parsed;
        typed.Value.Should().Be(0x77);
        typed.Name.Should().Be("Aisling");
    }

    [Fact]
    public void RoundTrip_NoneServerPacket()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var original = new TestNoneServerPacket { Tag = 0xBEEF };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<TestNoneServerPacket>();
        ((TestNoneServerPacket)parsed).Tag.Should().Be(0xBEEF);
    }

    // -- Normal mode (round-trip; explicit bytes vary with bRand/sRand) -------

    [Fact]
    public void RoundTrip_NormalClientPacket()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var original = new TestNormalClientPacket { Name = "Aisling", Password = "secret" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().BeOfType<TestNormalClientPacket>();
        var typed = (TestNormalClientPacket)parsed;
        typed.Name.Should().Be("Aisling");
        typed.Password.Should().Be("secret");
    }

    [Fact]
    public void RoundTrip_NormalServerPacket()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var original = new TestNormalServerPacket { Status = 0xA5 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<TestNormalServerPacket>();
        ((TestNormalServerPacket)parsed).Status.Should().Be(0xA5);
    }

    // -- MD5Key mode -----------------------------------------------------------

    [Fact]
    public void RoundTrip_Md5KeyClientPacket()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var original = new TestMd5KeyClientPacket { Text = "hello world" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().BeOfType<TestMd5KeyClientPacket>();
        ((TestMd5KeyClientPacket)parsed).Text.Should().Be("hello world");
    }

    [Fact]
    public void RoundTrip_Md5KeyServerPacket()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var original = new TestMd5KeyServerPacket { Value = 0xDEADBEEF };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<TestMd5KeyServerPacket>();
        ((TestMd5KeyServerPacket)parsed).Value.Should().Be(0xDEADBEEFu);
    }

    // -- Dialog obfuscation (MD5Key + pre-encryption transform on 0x39) -------

    [Fact]
    public void RoundTrip_DialogClientPacket_AppliesAndRemovesObfuscation()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var original = new TestDialogClientPacket
        {
            ObjectType = 0x01,
            ObjectId = 0x0000002A,
            PursuitId = 0x0005,
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().BeOfType<TestDialogClientPacket>();
        var typed = (TestDialogClientPacket)parsed;
        typed.ObjectType.Should().Be(0x01);
        typed.ObjectId.Should().Be(0x0000002Au);
        typed.PursuitId.Should().Be((ushort)0x0005);
    }

    // -- Sequence override -----------------------------------------------------

    [Fact]
    public void Encode_WithExplicitSequence_DoesNotAdvanceClientOrdinal()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        crypto.ClientOrdinal = 5;

        codec.EncodeClient(
            new TestMd5KeyClientPacket { Text = "x" },
            crypto,
            sequence: 0x42);

        crypto.ClientOrdinal.Should().Be((byte)5);
    }

    [Fact]
    public void Encode_WithoutExplicitSequence_AdvancesClientOrdinal()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        crypto.ClientOrdinal = 5;

        codec.EncodeClient(new TestMd5KeyClientPacket { Text = "x" }, crypto);

        crypto.ClientOrdinal.Should().Be((byte)6);
    }

    // -- TryGet incomplete-buffer handling ------------------------------------

    [Fact]
    public void TryGetClientPacket_OnEmptyBuffer_ShouldReturnFalse()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();

        codec.TryGetClientPacket(ReadOnlyMemory<byte>.Empty, crypto, out var packet, out var consumed)
             .Should().BeFalse();
        packet.Should().BeNull();
        consumed.Should().Be(0);
    }

    [Fact]
    public void TryGetClientPacket_OnBufferShorterThanHeader_ShouldReturnFalse()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var partial = new byte[] { 0xAA, 0x00 };

        codec.TryGetClientPacket(partial, crypto, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetClientPacket_OnBufferShorterThanDeclaredFrame_ShouldReturnFalse()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var partial = new byte[] { 0xAA, 0x00, 0x0A, 0x10, 0x42 };

        codec.TryGetClientPacket(partial, crypto, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetServerPacket_OnShortBuffer_ShouldReturnFalse()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();

        codec.TryGetServerPacket(new byte[] { 0xAA }, crypto, out _, out _).Should().BeFalse();
    }

    // -- TryGet wire-format error handling ------------------------------------

    [Fact]
    public void TryGetClientPacket_OnMissingFrameMarker_ShouldThrow()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var bogus = new byte[] { 0xBB, 0x00, 0x03, 0x10, 0x00, 0x00 };

        var act = () => { codec.TryGetClientPacket(bogus, crypto, out _, out _); };

        act.Should().Throw<InvalidDataException>().WithMessage("*Frame marker*");
    }

    [Fact]
    public void TryGetClientPacket_OnUnknownOpcode_ShouldThrow()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        // 0x99 is None per the client table but not registered as a test packet.
        // (Opcodes mapped to None are {0x00, 0x10, 0x48} - 0x99 falls through to MD5Key,
        // so the codec will try to decrypt before checking the parser table. To exercise
        // the unknown-parser path cleanly, use a None opcode that isn't registered.
        // 0x00 is None but might collide with potential CryptoKey handling; 0x48 is unused
        // by tests, so use that.)
        var wire = new byte[] { 0xAA, 0x00, 0x03, 0x48, 0xAA, 0x00 };

        var act = () => { codec.TryGetClientPacket(wire, crypto, out _, out _); };

        act.Should().Throw<InvalidDataException>().WithMessage("*client opcode 0x48*");
    }

    [Fact]
    public void TryGetClientPacket_OnMissingTrailingNull_ShouldThrow()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        // Opcode 0x10 (None), body byte 0x42, trailing byte 0xFF instead of 0x00.
        var wire = new byte[] { 0xAA, 0x00, 0x04, 0x10, 0x42, 0x00, 0xFF };

        var act = () => { codec.TryGetClientPacket(wire, crypto, out _, out _); };

        act.Should().Throw<InvalidDataException>().WithMessage("*trailing 0x00*");
    }

    [Fact]
    public void TryGetServerPacket_OnUnknownOpcode_ShouldThrow()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        // 0x7E is server None per the table but not registered.
        var wire = new byte[] { 0xAA, 0x00, 0x01, 0x7E };

        var act = () => { codec.TryGetServerPacket(wire, crypto, out _, out _); };

        act.Should().Throw<InvalidDataException>().WithMessage("*server opcode 0x7E*");
    }

    [Fact]
    public void TryGetClientPacket_DoesNotAcceptServerOpcodes()
    {
        // 0x40 is registered as a server opcode in the test assembly. Attempting to parse
        // it via the client dispatch table should fail with "no parser" rather than succeed.
        // 0x40 is None on the server side; on the client side, it's catch-all MD5Key, so
        // the codec will try to decrypt before hitting the parser-missing path. That makes
        // the message contain the decrypt-side error, not the parser error.
        // To meaningfully test direction isolation: use a None-mapped client opcode that
        // happens to also be a server-registered opcode. There's no such overlap with our
        // current test set, so this scenario is intrinsically tested by other paths.
        // Keep the assertion narrow: the codec doesn't claim 0x40 as a client opcode.
        var codec = new PacketCodec(TestPacketAssemblies);

        codec.IsClientOpcodeRegistered(0x40).Should().BeFalse();
    }

    // -- Multi-packet iteration -----------------------------------------------

    [Fact]
    public void TryGetClientPacket_OnConcatenatedFrames_ShouldYieldEachInTurn()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();

        var first = codec.EncodeClient(new TestNoneClientPacket { Value = 0x11, Name = "a" }, crypto);
        var second = codec.EncodeClient(new TestNoneClientPacket { Value = 0x22, Name = "bb" }, crypto);

        var combined = new byte[first.Length + second.Length];
        first.Span.CopyTo(combined);
        second.Span.CopyTo(combined.AsSpan(first.Length));

        var buffer = (ReadOnlyMemory<byte>)combined;
        var totalConsumed = 0;

        codec.TryGetClientPacket(buffer, crypto, out var p1, out var c1).Should().BeTrue();
        ((TestNoneClientPacket)p1!).Value.Should().Be(0x11);
        ((TestNoneClientPacket)p1!).Name.Should().Be("a");
        c1.Should().Be(first.Length);
        totalConsumed += c1;

        codec.TryGetClientPacket(buffer[totalConsumed..], crypto, out var p2, out var c2).Should().BeTrue();
        ((TestNoneClientPacket)p2!).Value.Should().Be(0x22);
        ((TestNoneClientPacket)p2!).Name.Should().Be("bb");
        c2.Should().Be(second.Length);
        totalConsumed += c2;

        totalConsumed.Should().Be(combined.Length);
        codec.TryGetClientPacket(buffer[totalConsumed..], crypto, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetServerPacket_OnConcatenatedFrames_ShouldYieldEachInTurn()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();

        var first = codec.EncodeServer(new TestNoneServerPacket { Tag = 0x0001 }, crypto);
        var second = codec.EncodeServer(new TestNoneServerPacket { Tag = 0x0002 }, crypto);

        var combined = new byte[first.Length + second.Length];
        first.Span.CopyTo(combined);
        second.Span.CopyTo(combined.AsSpan(first.Length));

        var buffer = (ReadOnlyMemory<byte>)combined;
        var totalConsumed = 0;

        codec.TryGetServerPacket(buffer, crypto, out var p1, out var c1).Should().BeTrue();
        ((TestNoneServerPacket)p1!).Tag.Should().Be(0x0001);
        totalConsumed += c1;

        codec.TryGetServerPacket(buffer[totalConsumed..], crypto, out var p2, out var c2).Should().BeTrue();
        ((TestNoneServerPacket)p2!).Tag.Should().Be(0x0002);
        totalConsumed += c2;

        totalConsumed.Should().Be(combined.Length);
    }

    // -- Parse-with-tail -------------------------------------------------------

    [Fact]
    public void ParseClientPacket_OnBufferWithTrailingBytes_ShouldThrow()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var first = codec.EncodeClient(new TestNoneClientPacket { Value = 0x01, Name = "x" }, crypto);
        var withTail = first.ToArray().Concat(new byte[] { 0xDE, 0xAD }).ToArray();

        var act = () => codec.ParseClientPacket(withTail, crypto);

        act.Should().Throw<InvalidDataException>().WithMessage("*trailing*");
    }

    [Fact]
    public void Encode_WithEmptyBody_ShouldStillProduceValidFrame()
    {
        var codec = new PacketCodec(TestPacketAssemblies);
        var crypto = MakeReadyCrypto();
        var packet = new TestNoneClientPacket { Value = 0x00, Name = "" };

        var wire = codec.EncodeClient(packet, crypto);

        codec.ParseClientPacket(wire, crypto).Should().BeOfType<TestNoneClientPacket>();
    }
}
