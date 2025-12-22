using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Editor.Avalonia.Services;

public sealed class TerminalGlyphCache
{
   private readonly IGlyphRepository _repo;
   private readonly FontStyleId _baseStyle = new FontStyleId("_base_");

   // key: pack|style|glyph
   private readonly Dictionary<string, GlyphBitmap> _cache = new Dictionary<string, GlyphBitmap>(StringComparer.Ordinal);

   public TerminalGlyphCache(IGlyphRepository repo)
   {
      _repo = repo ?? throw new ArgumentNullException(nameof(repo));
   }

   public async Task<GlyphBitmap> GetBitmapAsync(
       string packId,
       FontStyleId style,
       GlyphId resolvedId,
       GridSize expectedSize,
       GlyphId fallbackGlyph,
       CancellationToken ct)
   {
      var key = MakeKey(packId, style, resolvedId);
      if(_cache.TryGetValue(key, out var bmp))
         return bmp;

      // Try active style first, then _base_, then fallbackGlyph (all via repo)
      Glyph? glyph = null;

      if(await _repo.ExistsAsync(packId, style, resolvedId, ct).ConfigureAwait(false))
         glyph = await _repo.LoadAsync(packId, style, resolvedId, ct).ConfigureAwait(false);
      else if(style != _baseStyle && await _repo.ExistsAsync(packId, _baseStyle, resolvedId, ct).ConfigureAwait(false))
         glyph = await _repo.LoadAsync(packId, _baseStyle, resolvedId, ct).ConfigureAwait(false);
      else if(await _repo.ExistsAsync(packId, _baseStyle, fallbackGlyph, ct).ConfigureAwait(false))
         glyph = await _repo.LoadAsync(packId, _baseStyle, fallbackGlyph, ct).ConfigureAwait(false);

      // If still null, create empty bitmap (safe fallback)
      if(glyph == null || glyph.Bitmap == null || glyph.Bitmap.Data == null)
      {
         var empty = new byte[GlyphBitmap.GetByteLength(expectedSize)];
         bmp = new GlyphBitmap(expectedSize, empty);
      }
      else
      {
         // If stored glyph size differs, we still return it, but MVP expects consistent size.
         bmp = glyph.Bitmap;
      }

      _cache[key] = bmp;
      return bmp;
   }

   public void Clear() => _cache.Clear();

   private static string MakeKey(string packId, FontStyleId style, GlyphId id)
   {
      // GlyphId.ToString() is stable enough for cache key in tool; if you prefer, build your own.
      return packId + "|" + style.Name + "|" + id.ToString();
   }
}
