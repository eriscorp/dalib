using System;
using System.Collections.Generic;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x06 MapEdit (S->C). Body is [u8 StartX][u8 StartY][u8 Width][u8 Height] then
///     Width x Height tile cells of three big-endian u16s. Modeled for protocol completeness; not
///     emitted by typical servers.
/// </summary>
public class MapEditPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static MapEditPacket SampleTwoByOne() => new()
    {
        StartX = 0x0A,
        StartY = 0x0B,
        Width = 2,
        Height = 1,
        Tiles =
        [
            new MapEditTile { Background = 0x0102, LeftForeground = 0x0304, RightForeground = 0x0506 },
            new MapEditTile { Background = 0x0708, LeftForeground = 0x090A, RightForeground = 0x0B0C },
        ],
    };

    [Fact]
    public void WriteBody_PinsHeaderThenBigEndianCells()
    {
        SampleTwoByOne().ToBody().Should().Equal(
            (byte)0x0A, (byte)0x0B, (byte)0x02, (byte)0x01,                 // StartX, StartY, Width, Height
            (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04, (byte)0x05, (byte)0x06,  // tile 0
            (byte)0x07, (byte)0x08, (byte)0x09, (byte)0x0A, (byte)0x0B, (byte)0x0C); // tile 1
    }

    [Fact]
    public void Parse_ReadsHeaderAndGrid()
    {
        var parsed = MapEditPacket.Parse([
            0x0A, 0x0B, 0x02, 0x01,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
            0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C,
        ]);

        parsed.StartX.Should().Be((byte)0x0A);
        parsed.StartY.Should().Be((byte)0x0B);
        parsed.Width.Should().Be((byte)2);
        parsed.Height.Should().Be((byte)1);
        parsed.Tiles.Should().HaveCount(2);
        parsed.Tiles[0].Should().Be(new MapEditTile { Background = 0x0102, LeftForeground = 0x0304, RightForeground = 0x0506 });
        parsed.Tiles[1].Should().Be(new MapEditTile { Background = 0x0708, LeftForeground = 0x090A, RightForeground = 0x0B0C });
    }

    [Fact]
    public void WriteBody_TileCountMismatch_Throws()
    {
        var bad = new MapEditPacket
        {
            StartX = 0, StartY = 0, Width = 2, Height = 2, // expects 4 tiles
            Tiles = [new MapEditTile { Background = 0, LeftForeground = 0, RightForeground = 0 }],
        };

        var act = () => bad.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EmptyRectangle_RoundTrips()
    {
        var empty = new MapEditPacket { StartX = 1, StartY = 2, Width = 0, Height = 0, Tiles = [] };

        empty.ToBody().Should().Equal((byte)1, (byte)2, (byte)0, (byte)0);
        MapEditPacket.Parse([1, 2, 0, 0]).Tiles.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesGrid()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = SampleTwoByOne();

        var parsed = codec.ParseServerPacket(codec.EncodeServer(original, crypto), crypto)
            .Should().BeOfType<MapEditPacket>().Subject;

        parsed.StartX.Should().Be(original.StartX);
        parsed.StartY.Should().Be(original.StartY);
        parsed.Width.Should().Be(original.Width);
        parsed.Height.Should().Be(original.Height);
        parsed.Tiles.Should().Equal(original.Tiles);
    }
}
