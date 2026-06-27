using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

public class DrawObjectsPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_EmptyObjects_TwoZeroBytes()
    {
        var packet = new DrawObjectsPacket();
        packet.ToBody().Should().Equal((byte)0x00, (byte)0x00);
    }

    [Fact]
    public void WriteBody_SingleItem_Length15()
    {
        // u16 count = 1 (2 bytes) + 13 bytes item-shape = 15 bytes.
        var packet = new DrawObjectsPacket
        {
            Objects = [
                new ItemWorldObject
                {
                    X = 0x0102,
                    Y = 0x0304,
                    Id = 0xDEADBEEF,
                    Sprite = 0x8200,
                    Color = 7,
                },
            ],
        };

        var body = packet.ToBody();
        body.Length.Should().Be(15);

        // [count 00 01][X 01 02][Y 03 04][Id DE AD BE EF][Sprite 82 00][Color 07][Dir 00][Unk 00]
        body.Should().Equal(
            (byte)0x00, (byte)0x01,
            (byte)0x01, (byte)0x02,
            (byte)0x03, (byte)0x04,
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF,
            (byte)0x82, (byte)0x00,
            (byte)0x07,
            (byte)0x00,
            (byte)0x00);
    }

    [Fact]
    public void WriteBody_SingleCreatureUnnamed_Length19()
    {
        // u16 count = 1 (2 bytes) + 17 bytes creature-shape = 19 bytes.
        var packet = new DrawObjectsPacket
        {
            Objects = [
                new CreatureWorldObject
                {
                    X = 1,
                    Y = 2,
                    Id = 100,
                    Sprite = 0x4200,
                    Direction = 3,
                    Type = 0,
                },
            ],
        };

        packet.ToBody().Length.Should().Be(19);
    }

    [Fact]
    public void WriteBody_SingleCreatureNamed_LengthIncludesName()
    {
        var packet = new DrawObjectsPacket
        {
            Objects = [
                new CreatureWorldObject
                {
                    X = 1, Y = 2, Id = 100,
                    Sprite = 0x4500,
                    Type = CreatureWorldObject.TypeNamed,
                    Name = "Innkeeper",
                },
            ],
        };

        // Count(2) + creature header(17) + name length(1) + "Innkeeper"(9) = 29.
        packet.ToBody().Length.Should().Be(29);
    }

    [Fact]
    public void WriteBody_CreatureWithNameButTypeNotNamed_NameOmitted()
    {
        // Lossy by design: if Type != TypeNamed, the name doesn't travel.
        var packet = new DrawObjectsPacket
        {
            Objects = [
                new CreatureWorldObject
                {
                    X = 1, Y = 2, Id = 100,
                    Sprite = 0x4500,
                    Type = 0,
                    Name = "ShouldNotAppear",
                },
            ],
        };

        // Count(2) + creature header(17) = 19. No name bytes.
        packet.ToBody().Length.Should().Be(19);
    }

    [Fact]
    public void WriteBody_MixedBatch_MultipleObjects()
    {
        var packet = new DrawObjectsPacket
        {
            Objects = [
                new ItemWorldObject { X = 1, Y = 2, Id = 10, Sprite = 0x8100 },
                new CreatureWorldObject { X = 3, Y = 4, Id = 11, Sprite = 0x4100 },
                new ItemWorldObject { X = 5, Y = 6, Id = 12, Sprite = 0x8200 },
            ],
        };

        // Count(2) + 13 + 17 + 13 = 45 bytes.
        var body = packet.ToBody();
        body.Length.Should().Be(45);
        body[0].Should().Be(0x00);
        body[1].Should().Be(0x03);
    }

    [Fact]
    public void RoundTrip_ItemPreservesFields()
    {
        var original = new DrawObjectsPacket
        {
            Objects = [
                new ItemWorldObject
                {
                    X = 100, Y = 200, Id = 0xCAFEBABE,
                    Sprite = 0x9000, Color = 5,
                    Direction = 1, Unknown = 0xAA,
                },
            ],
        };

        var parsed = DrawObjectsPacket.Parse(original.ToBody());

        parsed.Objects.Should().HaveCount(1);
        var item = parsed.Objects[0].Should().BeOfType<ItemWorldObject>().Subject;
        item.X.Should().Be(100);
        item.Y.Should().Be(200);
        item.Id.Should().Be(0xCAFEBABE);
        item.Sprite.Should().Be(0x9000);
        item.Color.Should().Be(5);
        item.Direction.Should().Be(1);
        item.Unknown.Should().Be(0xAA);
    }

    [Fact]
    public void RoundTrip_CreatureNamedPreservesAllFields()
    {
        var original = new DrawObjectsPacket
        {
            Objects = [
                new CreatureWorldObject
                {
                    X = 50, Y = 60, Id = 999,
                    Sprite = 0x4700,
                    Slot0 = 1, Slot1 = 2, Slot2 = 3, Slot3 = 4,
                    Direction = 2, Unknown = 0,
                    Type = CreatureWorldObject.TypeNamed,
                    Name = "Aoife",
                },
            ],
        };

        var parsed = DrawObjectsPacket.Parse(original.ToBody());

        var creature = parsed.Objects[0].Should().BeOfType<CreatureWorldObject>().Subject;
        creature.X.Should().Be(50);
        creature.Sprite.Should().Be(0x4700);
        creature.Slot0.Should().Be(1);
        creature.Slot3.Should().Be(4);
        creature.Direction.Should().Be(2);
        creature.Type.Should().Be(CreatureWorldObject.TypeNamed);
        creature.Name.Should().Be("Aoife");
    }

    [Fact]
    public void Parse_DispatchesByspriteRange()
    {
        var packet = new DrawObjectsPacket
        {
            Objects = [
                new ItemWorldObject { X = 1, Y = 1, Id = 1, Sprite = 0x3FFF },     // just below creature range
                new CreatureWorldObject { X = 2, Y = 2, Id = 2, Sprite = 0x4000 }, // start of creature range
                new CreatureWorldObject { X = 3, Y = 3, Id = 3, Sprite = 0x7FFF }, // end of creature range
                new ItemWorldObject { X = 4, Y = 4, Id = 4, Sprite = 0x8000 },     // just above creature range
                new ItemWorldObject { X = 5, Y = 5, Id = 5, Sprite = 0xC000 },     // out-of-render upper range
            ],
        };

        var parsed = DrawObjectsPacket.Parse(packet.ToBody());

        parsed.Objects.Should().HaveCount(5);
        parsed.Objects[0].Should().BeOfType<ItemWorldObject>();
        parsed.Objects[1].Should().BeOfType<CreatureWorldObject>();
        parsed.Objects[2].Should().BeOfType<CreatureWorldObject>();
        parsed.Objects[3].Should().BeOfType<ItemWorldObject>();
        parsed.Objects[4].Should().BeOfType<ItemWorldObject>();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesBatch()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new DrawObjectsPacket
        {
            Objects = [
                new ItemWorldObject { X = 10, Y = 20, Id = 1, Sprite = 0x8100, Color = 2 },
                new CreatureWorldObject
                {
                    X = 30, Y = 40, Id = 2, Sprite = 0x4500,
                    Type = CreatureWorldObject.TypeNamed, Name = "Bandit",
                },
            ],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DrawObjectsPacket>().Subject;
        typed.Objects.Should().HaveCount(2);
        typed.Objects[0].Should().BeOfType<ItemWorldObject>().Which.Color.Should().Be(2);
        typed.Objects[1].Should().BeOfType<CreatureWorldObject>().Which.Name.Should().Be("Bandit");
    }

    [Fact]
    public void BuildUpStyle_AssembleObjectListIncrementally()
    {
        // Pin the build-up shape: assemble a list of objects from game state.
        var list = new List<WorldObject>();

        // A creature read from game state:
        list.Add(new CreatureWorldObject
        {
            X = 5, Y = 5, Id = 100,
            Sprite = 0x4200,
            Direction = 1,
        });

        // Then an item dropped at the same tile:
        list.Add(new ItemWorldObject
        {
            X = 5, Y = 5, Id = 101,
            Sprite = 0x9000, Color = 3,
        });

        var packet = new DrawObjectsPacket { Objects = list };
        var parsed = DrawObjectsPacket.Parse(packet.ToBody());

        parsed.Objects.Should().HaveCount(2);
        parsed.Objects[0].Should().BeOfType<CreatureWorldObject>();
        parsed.Objects[1].Should().BeOfType<ItemWorldObject>();
    }
}
