using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Core.Models
{
   public readonly struct GlyphId
   {
      public int? UnicodeCodePoint { get; }
      public string? InternalName { get; }

      public bool IsUnicode => UnicodeCodePoint.HasValue;
      public bool IsInternal => !string.IsNullOrWhiteSpace(InternalName);

      private GlyphId(int codePoint)
      {
         if(codePoint < 0 || codePoint > 0x10FFFF)
            throw new ArgumentOutOfRangeException(nameof(codePoint), "Unicode code point out of range.");

         // Exclude surrogate range
         if(codePoint >= 0xD800 && codePoint <= 0xDFFF)
            throw new ArgumentOutOfRangeException(nameof(codePoint), "Surrogate code points are not valid Unicode scalar values.");

         UnicodeCodePoint = codePoint;
         InternalName = null;
      }

      private GlyphId(string internalName)
      {
         internalName = (internalName ?? string.Empty).Trim();

         if(internalName.Length == 0)
            throw new ArgumentException("Internal glyph name cannot be empty.", nameof(internalName));

         UnicodeCodePoint = null;
         InternalName = internalName;
      }

      public static GlyphId FromUnicode(int codePoint) => new GlyphId(codePoint);

      public static GlyphId FromInternal(string name) => new GlyphId(name);

      public override string ToString()
          => IsUnicode
              ? $"U+{UnicodeCodePoint!.Value.ToString("X", CultureInfo.InvariantCulture)}"
              : $"_{InternalName}";
   }
}