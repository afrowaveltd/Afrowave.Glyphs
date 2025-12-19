using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
   public sealed class Glyph
   {
      public GlyphId Id { get; }
      public GridSize Size { get; }
      public GlyphBitmap Bitmap { get; }

      public Glyph(GlyphId id, GridSize size, GlyphBitmap bitmap)
      {
         Id = id;
         Size = size;
         Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));

         if(!bitmap.Size.Equals(size))
            throw new ArgumentException("Bitmap size must match glyph size.", nameof(bitmap));
         return;
      }
   }
}