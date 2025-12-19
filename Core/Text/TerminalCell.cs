using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Text
{
   public sealed class TerminalCell
   {
      public GlyphId Requested { get; }
      public GlyphId Resolved { get; }
      public bool IsFallback { get; }

      public TerminalCell(GlyphId requested, GlyphId resolved, bool isFallback)
      {
         Requested = requested;
         Resolved = resolved;
         IsFallback = isFallback;
      }
   }
}