using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Text
{
   public sealed class TextLayoutOptions
   {
      public bool Wrap { get; set; } = true;
      public bool Clip { get; set; } = true;
      public int TabWidth { get; set; } = 4;
   }
}