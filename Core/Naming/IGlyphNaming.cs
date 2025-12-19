using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Naming
{
   public interface IGlyphNaming
   {
      string ToFileName(GlyphId id);

      bool TryParseFileName(string fileName, out GlyphId id);
   }
}