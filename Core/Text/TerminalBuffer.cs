using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Text
{
   public sealed class TerminalBuffer
   {
      public int Width { get; }
      public int Height { get; }

      private readonly TerminalCell[,] _cells;

      public TerminalBuffer(int width, int height)
      {
         if(width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
         if(height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

         Width = width;
         Height = height;
         _cells = new TerminalCell[width, height];
      }

      public TerminalCell GetCell(int x, int y)
          => _cells[x, y];

      public void SetCell(int x, int y, TerminalCell cell)
          => _cells[x, y] = cell;
   }
}