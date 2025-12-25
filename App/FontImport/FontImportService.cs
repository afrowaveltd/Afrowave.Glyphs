using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.FontImport
{
   public sealed partial class FontImportService
   {
      private readonly IGlyphRepository _repo;
      private readonly FontGlyphRasterizer _rasterizer;

      public IGlyphRepository Repository => _repo;

      public FontImportService(IGlyphRepository repo, FontGlyphRasterizer rasterizer)
      {
         _repo = repo ?? throw new ArgumentNullException(nameof(repo));
         _rasterizer = rasterizer ?? throw new ArgumentNullException(nameof(rasterizer));
      }

      public async Task ImportUnicodeRangeAsync(
         string packId,
         FontStyleId style,
         string fontPath,
         IEnumerable<int> codePoints,
         GridSize size,
         Func<int, GlyphBitmap, Task<GlyphBitmap>>? perGlyphWizard,
         CancellationToken ct)
      {
         if(string.IsNullOrWhiteSpace(packId)) throw new ArgumentException("packId is null or empty.", nameof(packId));
         if(string.IsNullOrWhiteSpace(fontPath)) throw new ArgumentException("fontPath is null or empty.", nameof(fontPath));
         if(!File.Exists(fontPath)) throw new FileNotFoundException("Font file not found.", fontPath);

         foreach(var cp in codePoints)
         {
            ct.ThrowIfCancellationRequested();

            var bmp = await _rasterizer.RasterizeAsync(fontPath, cp, size, ct).ConfigureAwait(false);

            if(perGlyphWizard != null)
               bmp = await perGlyphWizard(cp, bmp).ConfigureAwait(false);

            var glyph = new Glyph(GlyphId.FromUnicode(cp), size, bmp.Clone());
            await _repo.SaveAsync(packId, style, glyph, ct).ConfigureAwait(false);
         }
      }
   }
}
