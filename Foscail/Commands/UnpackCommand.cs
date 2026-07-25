using System.Buffers.Binary;
using System.CommandLine;
using DALib.Data;
using Foscail.Conversion;

namespace Foscail.Commands;

/// <summary>
///     `foscail unpack` — extract every entry from one or more DAT archives into individual files,
///     optionally converting self-contained image assets to PNG.
/// </summary>
internal static class UnpackCommand
{
    // sotp.dat and friends share the .dat extension but are not asset archives; opening them as one misparses.
    private static readonly HashSet<string> NonArchiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "sotp.dat"
    };

    public static Command Build()
    {
        var inputArg = new Argument<string>("input")
        {
            Description = "A .dat archive, or a directory containing .dat archives."
        };

        var outputOpt = new Option<string?>("--output", "-o")
        {
            Description = "Output directory. Each archive is unpacked into its own subdirectory here. Defaults to ./foscail-out."
        };

        var convertOpt = new Option<bool>("--convert", "-c")
        {
            Description = "Also convert image assets to PNG alongside the raw files."
        };

        var animateOpt = new Option<bool>("--animate", "-a")
        {
            Description = "With --convert: additionally emit looping animated PNGs for effect (EFA) files and monster (MPF) sequences."
        };

        var gifOpt = new Option<bool>("--gif", "-g")
        {
            Description = "With --convert: additionally emit looping animated GIFs (256-color quantized) for the same animations."
        };

        var command = new Command("unpack",
            "Extract every entry from one or more DAT archives into individual files.");
        command.Add(inputArg);
        command.Add(outputOpt);
        command.Add(convertOpt);
        command.Add(animateOpt);
        command.Add(gifOpt);

        command.SetAction(parseResult =>
        {
            var input = parseResult.GetValue(inputArg)!;
            var output = parseResult.GetValue(outputOpt)
                         ?? Path.Combine(Environment.CurrentDirectory, "foscail-out");
            var convert = parseResult.GetValue(convertOpt);

            var animationFormats = (parseResult.GetValue(animateOpt) ? AnimationFormats.Apng : AnimationFormats.None)
                                   | (parseResult.GetValue(gifOpt) ? AnimationFormats.Gif : AnimationFormats.None);

            return Run(input, output, convert, animationFormats);
        });

        return command;
    }

    public static int Run(string input, string output, bool convert, AnimationFormats animationFormats = AnimationFormats.None)
    {
        var archives = ResolveArchives(input);

        if (archives.Count == 0)
        {
            Console.Error.WriteLine($"No .dat archive found at '{input}'.");
            return 1;
        }

        // sibling archives (khanpal.dat, hades.dat) resolve from the directory the input lives in
        var sourceDir = Directory.Exists(input) ? input : Path.GetDirectoryName(Path.GetFullPath(input));
        using var context = new ConversionContext(sourceDir);

        var totalEntries = 0;
        var totalImages = 0;
        var failures = 0;

        foreach (var archivePath in archives)
        {
            var fileName = Path.GetFileName(archivePath);

            if (NonArchiveNames.Contains(fileName))
            {
                Console.WriteLine($"  {fileName}: skipped (not an asset archive)");
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(archivePath);
            var destDir = Path.Combine(output, name);

            DataArchive archive;

            try
            {
                // the extended (obfuscated/zlib) layout can't be parsed yet; report it rather than
                // letting the legacy parser read its leading 0xFFFFFFFF as an entry count
                if (IsExtendedFormat(archivePath))
                {
                    Console.Error.WriteLine($"  ! {fileName}: extended-format archive is not supported yet");
                    failures++;

                    continue;
                }

                Directory.CreateDirectory(destDir);
                archive = DataArchive.FromFile(archivePath, memoryMapped: true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ! {fileName}: failed to open ({ex.Message})");
                failures++;

                continue;
            }

            using (archive)
            {
                try
                {
                    archive.ExtractTo(destDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  ! {fileName}: extraction failed ({ex.Message})");
                    failures++;

                    continue;
                }

                totalEntries += archive.Count;
                Console.WriteLine($"  {name}: {archive.Count} entries -> {destDir}");

                if (!convert)
                    continue;

                foreach (var entry in archive)
                {
                    try
                    {
                        totalImages += AssetConverter.ConvertEntry(entry, archive, name, context, destDir, animationFormats);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"    ! {entry.EntryName}: convert failed ({ex.Message})");
                        failures++;
                    }
                }

                try
                {
                    totalImages += AssetConverter.ConvertTiles(archive, name, context, destDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"    ! {name}: tile conversion failed ({ex.Message})");
                    failures++;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Done. {archives.Count} archive(s), {totalEntries} entries extracted"
                          + (convert ? $", {totalImages} image(s) converted" : "")
                          + (failures > 0 ? $", {failures} failure(s)" : "")
                          + ".");

        return failures > 0 ? 2 : 0;
    }

    private static List<string> ResolveArchives(string input)
    {
        if (Directory.Exists(input))
            return Directory.EnumerateFiles(input, "*.dat", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
                            .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                            .ToList();

        if (File.Exists(input))
            return [input];

        // allow passing an archive name without the extension
        if (File.Exists(input + ".dat"))
            return [input + ".dat"];

        return [];
    }

    // The extended (obfuscated/zlib) archive layout is signalled by a leading 0xFFFFFFFF word;
    // everything else is the legacy fixed-index layout.
    private static bool IsExtendedFormat(string path)
    {
        Span<byte> head = stackalloc byte[4];

        using var fs = File.OpenRead(path);

        return fs.ReadAtLeast(head, 4, throwOnEndOfStream: false) == 4
               && BinaryPrimitives.ReadUInt32LittleEndian(head) == 0xFFFFFFFF;
    }
}
