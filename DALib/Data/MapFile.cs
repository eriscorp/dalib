﻿using System;
using System.IO;
using System.Text;
using DALib.Extensions;
using DALib.Memory;

namespace DALib.Data;

public sealed class MapFile(int width, int height)
{
    public int Height { get; } = height;
    public MapTile[,] Tiles { get; } = new MapTile[width, height];
    public int Width { get; } = width;

    public MapFile(Stream stream, int width, int height)
        : this(width, height)
    {
        using var reader = new BinaryReader(stream, Encoding.Default, true);

        for (var y = 0; y < Height; ++y)
            for (var x = 0; x < Width; ++x)
            {
                var background = reader.ReadInt16();
                var leftForeground = reader.ReadInt16();
                var rightForeground = reader.ReadInt16();
                Tiles[x, y] = new MapTile(background, leftForeground, rightForeground);
            }
    }

    public MapFile(Span<byte> buffer, int width, int height)
        : this(width, height)
    {
        var reader = new SpanReader(Encoding.Default, buffer);

        for (var y = 0; y < Height; ++y)
            for (var x = 0; x < Width; ++x)
            {
                var background = reader.ReadInt16();
                var leftForeground = reader.ReadInt16();
                var rightForeground = reader.ReadInt16();
                Tiles[x, y] = new MapTile(background, leftForeground, rightForeground);
            }
    }

    public static MapFile FromFile(string path, int width, int height)
    {
        using var stream = File.Open(
            path.WithExtension(".map"),
            new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.SequentialScan,
                Share = FileShare.ReadWrite
            });

        return new MapFile(stream, width, height);
    }

    public MapTile this[int x, int y] => Tiles[x, y];
}

public sealed class MapTile
{
    public int Background { get; }

    public int LeftForeground { get; }

    public int RightForeground { get; }

    public MapTile(int background, int leftForeground, int rightForeground)
    {
        Background = background;
        LeftForeground = leftForeground;
        RightForeground = rightForeground;
    }
}