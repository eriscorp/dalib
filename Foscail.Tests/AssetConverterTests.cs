using DALib.Data;
using Foscail.Conversion;
using SkiaSharp;

namespace Foscail.Tests;

/// <summary>
///     Pixel-level coverage of the palette-dependent conversion conventions: each test gives distinct
///     palettes distinct colors, converts a synthetic asset, and asserts the rendered pixel — so a
///     wrong palette choice (off-by-one id, wrong table, wrong gender override) fails loudly.
/// </summary>
public class AssetConverterTests : IDisposable
{
    private static readonly SKColor Red = new(255, 0, 0);
    private static readonly SKColor Green = new(0, 255, 0);
    private static readonly SKColor Blue = new(0, 0, 255);

    private readonly string OutDir;
    private readonly string ScratchDir;

    public AssetConverterTests()
    {
        ScratchDir = Directory.CreateTempSubdirectory("foscail-convert-").FullName;
        OutDir = Path.Combine(ScratchDir, "out");
        Directory.CreateDirectory(OutDir);
    }

    public void Dispose() => Directory.Delete(ScratchDir, true);

    private string WriteDat(string fileName, byte[] bytes)
    {
        var path = Path.Combine(ScratchDir, fileName);
        File.WriteAllBytes(path, bytes);

        return path;
    }

    private int ConvertAll(string datPath)
    {
        using var archive = DataArchive.FromFile(datPath);
        using var context = new ConversionContext(ScratchDir);

        var name = Path.GetFileNameWithoutExtension(datPath);

        return archive.Sum(entry => AssetConverter.ConvertEntry(entry, archive, name, context, OutDir));
    }

    [Fact]
    public void Effect_Epf_Uses_EffPal_Keyed_By_Effect_Number()
    {
        // effect 1 -> palette 1 (red); palette 0 is green so a missed lookup shows up as green
        var dat = WriteDat(
            "effects.dat",
            SyntheticData.BuildArchive(
                ("efct001.epf", SyntheticData.BuildEpf()),
                ("effpal.tbl", "1 1\n"u8.ToArray()),
                ("eff000.pal", SyntheticData.BuildPalette(Green)),
                ("eff001.pal", SyntheticData.BuildPalette(Red))));

        var written = ConvertAll(dat);

        written.Should().Be(1);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "efct001.epf.png"), 0, 0).Should().Be(Red);
    }

    [Fact]
    public void Item_Sheet_Uses_Global_Icon_Numbering_With_Fixed_Stride()
    {
        // sheet capacity is a fixed 266 regardless of the file's actual frame count (item041 in the
        // retail data has 247 frames): file 2's frames are global icons 267 and 268. Icon 267 ->
        // palette 1 (red), icon 268 falls through to palette 0 (blue). A per-file-count stride or
        // per-file keying would color both alike.
        var dat = WriteDat(
            "icons.dat",
            SyntheticData.BuildArchive(
                ("item002.epf", SyntheticData.BuildEpf(frameCount: 2)),
                ("itempal.tbl", "267 1\n"u8.ToArray()),
                ("item000.pal", SyntheticData.BuildPalette(Blue)),
                ("item001.pal", SyntheticData.BuildPalette(Red))));

        var written = ConvertAll(dat);

        written.Should().Be(2);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "item002.epf.000.png"), 0, 0).Should().Be(Red);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "item002.epf.001.png"), 0, 0).Should().Be(Blue);
    }

    [Fact]
    public void Khan_Epf_Resolves_Through_Sibling_Khanpal_With_Gender_Overrides()
    {
        // khanpal.dat is a sibling archive: part 1 defaults to palette 0 (green), male override
        // palette 1 (red), female override palette 2 (blue)
        WriteDat(
            "khanpal.dat",
            SyntheticData.BuildArchive(
                ("palu.tbl", "1 0\n1 1 -1\n1 2 -2\n"u8.ToArray()),
                ("palu000.pal", SyntheticData.BuildPalette(Green)),
                ("palu001.pal", SyntheticData.BuildPalette(Red)),
                ("palu002.pal", SyntheticData.BuildPalette(Blue))));

        // names are <gender><part><sprite:D3><anim>: both entries are SPRITE 1 (anim 01 / anim b) —
        // a parse that swallowed the anim digits into the sprite id would miss the tables entirely
        var dat = WriteDat(
            "parts.dat",
            SyntheticData.BuildArchive(
                ("mu00101.epf", SyntheticData.BuildEpf()),
                ("wu001b.epf", SyntheticData.BuildEpf())));

        var written = ConvertAll(dat);

        written.Should().Be(2);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "mu00101.epf.png"), 0, 0).Should().Be(Red);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "wu001b.epf.png"), 0, 0).Should().Be(Blue);
    }

    [Fact]
    public void Khan_Letter_Aliases_Resolve_To_Shared_Families()
    {
        // 'a' has no pala.* family — it routes to palb, per the client's own letter mapping
        WriteDat(
            "khanpal.dat",
            SyntheticData.BuildArchive(
                ("palb.tbl", "1 1\n"u8.ToArray()),
                ("palb000.pal", SyntheticData.BuildPalette(Green)),
                ("palb001.pal", SyntheticData.BuildPalette(Red))));

        var dat = WriteDat(
            "parts.dat",
            SyntheticData.BuildArchive(("ma00101.epf", SyntheticData.BuildEpf())));

        ConvertAll(dat).Should().Be(1);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "ma00101.epf.png"), 0, 0).Should().Be(Red);
    }

    [Fact]
    public void Khan_Body_Letter_Uses_Skin_Tone_Ramp_Default()
    {
        // body ('m') and face ('o') letters use the palm0-9 skin ramp, which has no TBL: every id
        // resolves to palette 0, the default tone
        WriteDat(
            "khanpal.dat",
            SyntheticData.BuildArchive(("palm0.pal", SyntheticData.BuildPalette(Green))));

        var dat = WriteDat(
            "body.dat",
            SyntheticData.BuildArchive(("mm00001.epf", SyntheticData.BuildEpf())));

        ConvertAll(dat).Should().Be(1);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "mm00001.epf.png"), 0, 0).Should().Be(Green);
    }

    [Fact]
    public void Khan_Epf_Without_Khanpal_Sibling_Stays_Raw()
    {
        var dat = WriteDat(
            "parts.dat",
            SyntheticData.BuildArchive(("mu00001.epf", SyntheticData.BuildEpf())));

        ConvertAll(dat).Should().Be(0);
    }

    [Fact]
    public void Unrecognized_Epf_Family_Is_Left_Raw()
    {
        var dat = WriteDat(
            "ui.dat",
            SyntheticData.BuildArchive(("album.epf", SyntheticData.BuildEpf())));

        ConvertAll(dat).Should().Be(0);
    }

    [Fact]
    public void Single_Palette_Era_Archive_Falls_Back_To_Legend_Pal()
    {
        // the 3.61 merged client carries legend.pal and no family tables: every EPF renders
        // through the global palette, including generic UI names and khan names with no khanpal
        var dat = WriteDat(
            "old.dat",
            SyntheticData.BuildArchive(
                ("legend.pal", SyntheticData.BuildPalette(Red)),
                ("backgrnd.epf", SyntheticData.BuildEpf()),
                ("ma00101.epf", SyntheticData.BuildEpf())));

        ConvertAll(dat).Should().Be(2);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "backgrnd.epf.png"), 0, 0).Should().Be(Red);
        SyntheticData.ReadPixel(Path.Combine(OutDir, "ma00101.epf.png"), 0, 0).Should().Be(Red);
    }

    [Fact]
    public void Modern_Archive_With_Family_Tables_Never_Falls_Back()
    {
        // 7.41's Legend.dat has legend.pal AND itempal.tbl — unknown families must stay raw there
        var dat = WriteDat(
            "modern.dat",
            SyntheticData.BuildArchive(
                ("legend.pal", SyntheticData.BuildPalette(Red)),
                ("itempal.tbl", "1 0\n"u8.ToArray()),
                ("item000.pal", SyntheticData.BuildPalette(Blue)),
                ("album.epf", SyntheticData.BuildEpf())));

        ConvertAll(dat).Should().Be(0);
    }
}
