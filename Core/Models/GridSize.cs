using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
   public readonly struct GridSize
   {
      public int Width { get; }
      public int Height { get; }

      public int CellCount => checked(Width * Height);

      public GridSize(int width, int height)
      {
         if(width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be > 0.");
         if(height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be > 0.");
         Width = width;
         Height = height;
      }

      public override string ToString() => $"{Width}x{Height}";
   }
}