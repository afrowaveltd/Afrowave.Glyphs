using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.Abstractions.Models
{
   public sealed class GlyphInfo
   {
      public GlyphId Id { get; }
      public bool Exists { get; }

      public GlyphInfo(GlyphId id, bool exists)
      {
         Id = id;
         Exists = exists;
      }
   }
}