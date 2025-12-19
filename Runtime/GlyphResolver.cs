using Core.Models;
using Core.Resolution;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime
{
   public sealed class GlyphResolver
   {
      private readonly IGlyphRepository _repository;
      private readonly FontStyleId _baseStyle;

      public GlyphResolver(IGlyphRepository repository)
      {
         _repository = repository ?? throw new ArgumentNullException(nameof(repository));
         _baseStyle = new FontStyleId("_base_");
      }

      public async Task<GlyphResolutionResult> ResolveAsync(
          string packId,
          FontStyleId activeStyle,
          GlyphId requested,
          GlyphId fallbackGlyph,
          CancellationToken ct)
      {
         // 1) Active style
         if(await _repository.ExistsAsync(packId, activeStyle, requested, ct))
            return new GlyphResolutionResult(requested, false);

         // 2) Base layer
         if(activeStyle != _baseStyle &&
             await _repository.ExistsAsync(packId, _baseStyle, requested, ct))
            return new GlyphResolutionResult(requested, true);

         // 3) Fallback glyph
         return new GlyphResolutionResult(fallbackGlyph, true);
      }
   }
}