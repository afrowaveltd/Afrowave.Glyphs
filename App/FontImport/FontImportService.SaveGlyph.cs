using Core.Models;
using Storage.Abstractions.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.FontImport
{
   public sealed partial class FontImportService
   {
      public Task SaveGlyphAsync(string packId, FontStyleId style, GlyphId id, GridSize size, GlyphBitmap bitmap, CancellationToken ct)
      {
         var glyph = new Glyph(id, size, bitmap.Clone());
         return _repo.SaveAsync(packId, style, glyph, ct);
      }
   }
}
