using DALib.Networking.Crypto;

namespace DALib.Tests.Networking.Crypto;

/// <summary>
///     Tests for <see cref="DialogObfuscation" />:
///     <list type="bullet">
///         <item>Round-trip: <c>Remove(Apply(body)) == body</c> for several body shapes.</item>
///         <item>Byte-for-byte equivalence with an independent reference implementation of the
///             dialog header/encrypt routine (inlined here as a test helper) under a fixed RNG seed.</item>
///         <item>CRC tamper detection in <see cref="DialogObfuscation.Remove" />.</item>
///         <item>The <see cref="DialogObfuscation.AppliesTo" /> opcode set.</item>
///     </list>
/// </summary>
public class DialogObfuscationTests
{
    [Fact]
    public void AppliesTo_OnlyDialogOpcodes()
    {
        DialogObfuscation.AppliesTo(0x39).Should().BeTrue();
        DialogObfuscation.AppliesTo(0x3A).Should().BeTrue();
        DialogObfuscation.AppliesTo(0x00).Should().BeFalse();
        DialogObfuscation.AppliesTo(0x38).Should().BeFalse();
        DialogObfuscation.AppliesTo(0x3B).Should().BeFalse();
        DialogObfuscation.AppliesTo(0xFF).Should().BeFalse();
    }

    // Typical 0x39 NpcMainMenu body: [byte objectType][u32 objectId][u16 pursuitId].
    public static TheoryData<byte[]> RoundTripBodies() => new()
    {
        new byte[] { },                                                          // empty
        new byte[] { 0x42 },                                                     // single byte
        new byte[] { 0x01, 0x00, 0x00, 0x00, 0x2A, 0x00, 0x05 },                 // 7-byte NpcMainMenu
        new byte[] { 0x01, 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x23, 0x00, 0x00, 0x42 },// DialogUse-ish
        Enumerable.Range(0, 200).Select(i => (byte)i).ToArray()                  // 200 bytes, all distinct
    };

    [Theory]
    [MemberData(nameof(RoundTripBodies))]
    public void Apply_Then_Remove_YieldsOriginalBody(byte[] body)
    {
        var obfuscated = DialogObfuscation.Apply(body, new Random(0xBEEF));
        obfuscated.Length.Should().Be(6 + body.Length);

        var recovered = DialogObfuscation.Remove(obfuscated);
        recovered.Should().Equal(body);
    }

    [Fact]
    public void Apply_WithSeededRng_MatchesServerAlgorithm()
    {
        // A typical 0x39 payload - objectType=1, objectId=0x2A, pursuitId=5.
        var body = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x2A, 0x00, 0x05 };

        // Same seed - both Apply and the reference impl draw identical random bytes.
        var ourOutput = DialogObfuscation.Apply(body, new Random(0xBEEF));
        var referenceOutput = ReferenceEncrypt(body, new Random(0xBEEF));

        ourOutput.Should().Equal(referenceOutput);
    }

    [Fact]
    public void Remove_ThrowsOnCrcMismatch()
    {
        var body = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x2A, 0x00, 0x05 };
        var obfuscated = DialogObfuscation.Apply(body, new Random(0xBEEF));

        // Flip a bit in the body area (obfuscated[6..]). It stays flipped after
        // unmasking because the XOR mask for position i is deterministic from the
        // header random bytes, which we haven't touched.
        obfuscated[6] ^= 0x01;

        var act = () => DialogObfuscation.Remove(obfuscated);

        act.Should().Throw<InvalidDataException>().WithMessage("*CRC mismatch*");
    }

    [Fact]
    public void Remove_ThrowsOnUndersizedBuffer()
    {
        var act = () => DialogObfuscation.Remove(new byte[5]);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Apply_EmptyBody_Produces6BytesAndRoundTrips()
    {
        // CRC-16-CCITT of zero bytes is 0x0000. The 6-byte header for an empty body has
        // masked [2..3] = (0x0002 ^ y_mask) and [4..5] = (0x0000 ^ z_mask). Explicit
        // CRC bytes vary with the random seed; round-tripping is the meaningful check.
        var output = DialogObfuscation.Apply(Array.Empty<byte>(), new Random(42));

        output.Length.Should().Be(6);
        DialogObfuscation.Remove(output).Should().BeEmpty();
    }

    // Reference implementation
    //
    // An independent reference implementation of the dialog header + encrypt routine,
    // used to verify that Apply matches independently-derived output. Intentionally kept
    // separate from the class under test rather than sharing any helpers.

    private static byte[] ReferenceEncrypt(byte[] body, Random rng)
    {
        var data = new byte[6 + body.Length];
        Array.Copy(body, 0, data, 6, body.Length);

        // Header generation.
        ushort crc = 0;
        for (var i = 0; i < data.Length - 6; i++)
            crc = (ushort)(data[6 + i] ^ (ushort)(crc << 8) ^ DialogCrcTable[crc >> 8]);
        data[0] = (byte)rng.Next(256);
        data[1] = (byte)rng.Next(256);
        data[2] = (byte)((data.Length - 4) / 256);
        data[3] = (byte)((data.Length - 4) % 256);
        data[4] = (byte)(crc / 256);
        data[5] = (byte)(crc % 256);

        // Encrypt step.
        var length = (data[2] << 8) | data[3];
        var xPrime = (byte)(data[0] - 0x2D);
        var x = (byte)(data[1] ^ xPrime);
        var y = (byte)(x + 0x72);
        var z = (byte)(x + 0x28);
        data[2] ^= y;
        data[3] ^= (byte)((y + 1) % 256);
        for (var i = 0; i < length; i++)
            data[4 + i] ^= (byte)((z + i) % 256);

        return data;
    }

    // A standalone CRC-16-CCITT lookup table. Kept separate from the class under test
    // (which uses the dynamically-built CrcCcitt.Table) so a bug in either would surface
    // as a reference-vs-impl mismatch.
    private static readonly ushort[] DialogCrcTable =
    {
        0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7,
        0x8108, 0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF,
        0x1231, 0x0210, 0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6,
        0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE,
        0x2462, 0x3443, 0x0420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485,
        0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D,
        0x3653, 0x2672, 0x1611, 0x0630, 0x76D7, 0x66F6, 0x5695, 0x46B4,
        0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D, 0xC7BC,
        0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861, 0x2802, 0x3823,
        0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
        0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12,
        0xDBFD, 0xCBDC, 0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A,
        0x6CA6, 0x7C87, 0x4CE4, 0x5CC5, 0x2C22, 0x3C03, 0x0C60, 0x1C41,
        0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B, 0x8D68, 0x9D49,
        0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0x0E70,
        0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78,
        0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F,
        0x1080, 0x00A1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
        0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E,
        0x02B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
        0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D,
        0x34E2, 0x24C3, 0x14A0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
        0xA7DB, 0xB7FA, 0x8799, 0x97B8, 0xE75F, 0xF77E, 0xC71D, 0xD73C,
        0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657, 0x7676, 0x4615, 0x5634,
        0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB,
        0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882, 0x28A3,
        0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A,
        0x4A75, 0x5A54, 0x6A37, 0x7A16, 0x0AF1, 0x1AD0, 0x2AB3, 0x3A92,
        0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9,
        0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1,
        0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8,
        0x6E17, 0x7E36, 0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0x0ED1, 0x1EF0
    };
}
