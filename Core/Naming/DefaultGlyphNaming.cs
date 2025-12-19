using Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Core.Naming
{
   public sealed class DefaultGlyphNaming : IGlyphNaming
   {
      public const string Extension = ".glyph";

      public string ToFileName(GlyphId id)
      {
         if(id.IsUnicode)
         {
            var hex = id.UnicodeCodePoint!.Value.ToString("X", CultureInfo.InvariantCulture);
            return hex + Extension;
         }

         if(id.IsInternal)
         {
            return "_" + id.InternalName + Extension;
         }

         throw new InvalidOperationException("GlyphId is neither Unicode nor Internal.");
      }

      public bool TryParseFileName(string fileName, out GlyphId id)
      {
         id = default;

         if(string.IsNullOrWhiteSpace(fileName))
            return false;

         fileName = fileName.Trim();

         if(!fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            return false;

         var stem = fileName[..^Extension.Length];

         if(stem.Length == 0)
            return false;

         if(stem[0] == '_')
         {
            var name = stem[1..].Trim();
            if(name.Length == 0) return false;

            id = GlyphId.FromInternal(name);
            return true;
         }

         // Hex unicode codepoint
         if(!int.TryParse(stem, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var codePoint))
            return false;

         try
         {
            id = GlyphId.FromUnicode(codePoint);
            return true;
         }
         catch
         {
            return false;
         }
      }
   }
}