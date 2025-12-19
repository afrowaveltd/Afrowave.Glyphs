using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Resolution
{
   public sealed class GlyphResolutionResult
   {
      public GlyphId ResolvedId { get; }
      public bool IsFallback { get; }

      public GlyphResolutionResult(GlyphId resolvedId, bool isFallback)
      {
         ResolvedId = resolvedId;
         IsFallback = isFallback;
      }
   }
}