using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.FontImport
{
    public sealed partial class FontImportService
    {
        /// <summary>
        /// Import all available glyphs from a font, skipping missing or empty ones
        /// </summary>
        public async Task<ImportResult> ImportAllAvailableAsync(
            string packId,
            FontStyleId style,
            string fontPath,
            GridSize size,
            IProgress<ImportProgress>? progress,
            CancellationToken ct)
        {
            var result = new ImportResult();
            
            // Try common Unicode ranges
            var ranges = new List<(int start, int end, string name)>
            {
                (0x0020, 0x007F, "Basic Latin"),           // ASCII
                (0x0080, 0x00FF, "Latin-1 Supplement"),
                (0x0100, 0x017F, "Latin Extended-A"),
                (0x0180, 0x024F, "Latin Extended-B"),
                (0x0250, 0x02AF, "IPA Extensions"),
                (0x02B0, 0x02FF, "Spacing Modifier Letters"),
                (0x0300, 0x036F, "Combining Diacritical Marks"),
                (0x0370, 0x03FF, "Greek and Coptic"),
                (0x0400, 0x04FF, "Cyrillic"),
                (0x2000, 0x206F, "General Punctuation"),
                (0x2070, 0x209F, "Superscripts and Subscripts"),
                (0x20A0, 0x20CF, "Currency Symbols"),
                (0x2100, 0x214F, "Letterlike Symbols"),
                (0x2190, 0x21FF, "Arrows"),
                (0x2200, 0x22FF, "Mathematical Operators"),
                (0x2300, 0x23FF, "Miscellaneous Technical"),
                (0x2500, 0x257F, "Box Drawing"),
                (0x2580, 0x259F, "Block Elements"),
                (0x25A0, 0x25FF, "Geometric Shapes"),
                (0x2600, 0x26FF, "Miscellaneous Symbols"),
            };

            int totalChecked = 0;
            int totalRanges = ranges.Count;
            int currentRange = 0;

            foreach (var (start, end, name) in ranges)
            {
                ct.ThrowIfCancellationRequested();
                currentRange++;

                for (int cp = start; cp <= end; cp++)
                {
                    ct.ThrowIfCancellationRequested();
                    totalChecked++;

                    try
                    {
                        var bmp = await _rasterizer.RasterizeAsync(fontPath, cp, size, ct).ConfigureAwait(false);
                        
                        // Skip empty glyphs (all zeros)
                        if (IsEmptyGlyph(bmp))
                        {
                            result.Skipped++;
                            continue;
                        }

                        var glyph = new Glyph(GlyphId.FromUnicode(cp), size, bmp);
                        await _repo.SaveAsync(packId, style, glyph, ct).ConfigureAwait(false);
                        
                        result.Imported++;
                        result.ImportedCodePoints.Add(cp);

                        // Report progress every 10 glyphs
                        if (result.Imported % 10 == 0)
                        {
                            progress?.Report(new ImportProgress
                            {
                                CurrentRange = name,
                                RangeProgress = currentRange,
                                TotalRanges = totalRanges,
                                ImportedCount = result.Imported,
                                SkippedCount = result.Skipped,
                                CurrentChar = char.ConvertFromUtf32(cp)
                            });
                        }
                    }
                    catch
                    {
                        // Skip characters that can't be rasterized
                        result.Skipped++;
                    }
                }
            }

            progress?.Report(new ImportProgress
            {
                CurrentRange = "Complete",
                RangeProgress = totalRanges,
                TotalRanges = totalRanges,
                ImportedCount = result.Imported,
                SkippedCount = result.Skipped,
                CurrentChar = "✓"
            });

            return result;
        }

        /// <summary>
        /// Fill missing glyphs from another pack/style
        /// </summary>
        public async Task<FillMissingResult> FillMissingGlyphsAsync(
            string targetPackId,
            FontStyleId targetStyle,
            string sourcePackId,
            FontStyleId sourceStyle,
            GridSize size,
            IProgress<string>? progress,
            CancellationToken ct)
        {
            var result = new FillMissingResult();
            
            // Get all existing glyphs in target
            var existingIds = new HashSet<GlyphId>();
            try
            {
                var existing = await _repo.ListGlyphsAsync(targetPackId, targetStyle, ct).ConfigureAwait(false);
                foreach (var info in existing)
                {
                    existingIds.Add(info.Id);
                }
            }
            catch
            {
                // Target might not exist yet
            }

            // Get all glyphs from source
            var sourceGlyphs = await _repo.ListGlyphsAsync(sourcePackId, sourceStyle, ct).ConfigureAwait(false);
            
            foreach (var glyphInfo in sourceGlyphs)
            {
                ct.ThrowIfCancellationRequested();

                // Skip if already exists in target
                if (existingIds.Contains(glyphInfo.Id))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                            // Load from source
                            var sourceGlyph = await _repo.LoadAsync(sourcePackId, sourceStyle, glyphInfo.Id, ct).ConfigureAwait(false);

                            if (sourceGlyph == null || IsEmptyGlyph(sourceGlyph.Bitmap))
                            {
                                result.Skipped++;
                                continue;
                            }

                            // Save to target
                            await _repo.SaveAsync(targetPackId, targetStyle, sourceGlyph, ct).ConfigureAwait(false);

                            result.Filled++;
                            progress?.Report($"Filled: {glyphInfo.Id} ({result.Filled} total)");
                        }
                        catch
                        {
                            result.Failed++;
                        }
                    }

            return result;
        }

        /// <summary>
        /// Check if glyph is empty (all pixels off)
        /// </summary>
        private bool IsEmptyGlyph(GlyphBitmap bitmap)
        {
            for (int y = 0; y < bitmap.Size.Height; y++)
            {
                for (int x = 0; x < bitmap.Size.Width; x++)
                {
                    if (bitmap.GetPixel(x, y))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculate visual similarity between two glyphs (0.0 = identical, 1.0 = completely different)
        /// </summary>
        public double CalculateSimilarity(GlyphBitmap a, GlyphBitmap b)
        {
            if (!a.Size.Equals(b.Size))
                return 1.0; // Different sizes = completely different

            int totalPixels = a.Size.Width * a.Size.Height;
            int differentPixels = 0;

            for (int y = 0; y < a.Size.Height; y++)
            {
                for (int x = 0; x < a.Size.Width; x++)
                {
                    if (a.GetPixel(x, y) != b.GetPixel(x, y))
                        differentPixels++;
                }
            }

            return (double)differentPixels / totalPixels;
        }
    }

    public class ImportResult
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<int> ImportedCodePoints { get; } = new List<int>();
    }

    public class ImportProgress
    {
        public string CurrentRange { get; set; } = "";
        public int RangeProgress { get; set; }
        public int TotalRanges { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public string CurrentChar { get; set; } = "";
    }

    public class FillMissingResult
    {
        public int Filled { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
    }
}
