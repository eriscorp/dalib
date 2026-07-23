using System.Text;
using DALib.Data;

namespace DALib.Tests;

/// <summary>
///     Covers the DataArchive hardening around hostile or non-legacy input: entry names that would
///     escape the extraction directory, the extended-format (leading 0xFFFFFFFF) layout that the
///     legacy parser would otherwise read as an empty archive, and path handling for files whose
///     extension casing differs from ".dat".
/// </summary>
public class DataArchiveTests
{
    // legacy index layout: [count+1:i32] then per entry [start:i32][name:13 ascii, NUL padded],
    // a final [end:i32], then the concatenated entry data
    private static byte[] BuildArchive(params (string Name, byte[] Data)[] entries)
    {
        const int NAME_LENGTH = 13;

        using var ms = new MemoryStream();

        using (var writer = new BinaryWriter(ms, Encoding.Default, true))
        {
            writer.Write(entries.Length + 1);

            var address = 4 + entries.Length * (4 + NAME_LENGTH) + 4;

            foreach (var (name, data) in entries)
            {
                writer.Write(address);
                writer.Write(Encoding.ASCII.GetBytes(name.PadRight(NAME_LENGTH, '\0')));

                address += data.Length;
            }

            writer.Write(address);

            foreach (var (_, data) in entries)
                writer.Write(data);
        }

        return ms.ToArray();
    }

    private static string WriteTempFile(byte[] bytes, string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dalib-archive-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, bytes);

        return path;
    }

    [Fact]
    public void ExtractTo_Extracts_Normal_Entries()
    {
        var payload = "hello"u8.ToArray();
        var path = WriteTempFile(BuildArchive(("a.txt", payload)), ".dat");
        var destDir = Directory.CreateTempSubdirectory("dalib-extract-").FullName;

        try
        {
            using var archive = DataArchive.FromFile(path);

            archive.Count.Should().Be(1);
            archive.ExtractTo(destDir);

            File.ReadAllBytes(Path.Combine(destDir, "a.txt")).Should().BeEquivalentTo(payload);
        } finally
        {
            File.Delete(path);
            Directory.Delete(destDir, true);
        }
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("/tmp/evil")]
    public void ExtractTo_Rejects_EntryNames_That_Escape_The_Destination(string entryName)
    {
        var path = WriteTempFile(BuildArchive((entryName, [1, 2, 3])), ".dat");

        // nested destination so a "../" escape would land in scratchDir, where we can check for it
        var scratchDir = Directory.CreateTempSubdirectory("dalib-escape-").FullName;
        var destDir = Path.Combine(scratchDir, "out");
        Directory.CreateDirectory(destDir);

        try
        {
            using var archive = DataArchive.FromFile(path);

            var extract = () => archive.ExtractTo(destDir);

            extract.Should().Throw<InvalidDataException>();
            File.Exists(Path.Combine(scratchDir, "evil")).Should().BeFalse();
        } finally
        {
            File.Delete(path);
            Directory.Delete(scratchDir, true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FromFile_Throws_On_Negative_Entry_Count(bool memoryMapped)
    {
        // the extended-format layout leads with 0xFFFFFFFF; the legacy parser must reject it
        // instead of reading it as a count and yielding an empty archive
        var path = WriteTempFile([0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00], ".dat");

        try
        {
            var open = () => DataArchive.FromFile(path, memoryMapped);

            open.Should().Throw<InvalidDataException>();
        } finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromFile_Honors_Path_With_Uppercase_Extension()
    {
        // FromFile used to rewrite "X.DAT" to "X.dat", which does not exist on case-sensitive filesystems
        var path = WriteTempFile(BuildArchive(("a.txt", [1])), ".DAT");

        try
        {
            using var archive = DataArchive.FromFile(path);

            archive.Count.Should().Be(1);
        } finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromFile_Still_Appends_Extension_To_Extensionless_Path()
    {
        var path = WriteTempFile(BuildArchive(("a.txt", [1])), ".dat");

        try
        {
            using var archive = DataArchive.FromFile(path[..^4]);

            archive.Count.Should().Be(1);
        } finally
        {
            File.Delete(path);
        }
    }
}
