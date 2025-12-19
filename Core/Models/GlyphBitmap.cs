using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
   /// <summary>
   /// 1-bit-per-pixel bitmap. Pixel is either on/off.
   /// Data is packed: each byte holds 8 pixels, LSB first.
   /// </summary>
   public sealed class GlyphBitmap
   {
      public GridSize Size { get; }
      public byte[] Data { get; }

      public GlyphBitmap(GridSize size, byte[] data)
      {
         Size = size;
         Data = data ?? throw new ArgumentNullException(nameof(data));

         var expectedBytes = GetByteLength(size);
         if(Data.Length != expectedBytes)
            throw new ArgumentException($"Invalid data length. Expected {expectedBytes} bytes for size {size}, got {Data.Length}.", nameof(data));
      }

      public static int GetByteLength(GridSize size)
      {
         var bits = size.CellCount;
         return (bits + 7) / 8;
      }

      public bool GetPixel(int x, int y)
      {
         ValidateXY(x, y);

         var index = (y * Size.Width) + x;
         var byteIndex = index >> 3;
         var bitIndex = index & 7;

         return (Data[byteIndex] & (1 << bitIndex)) != 0;
      }

      public void SetPixel(int x, int y, bool on)
      {
         ValidateXY(x, y);

         var index = (y * Size.Width) + x;
         var byteIndex = index >> 3;
         var bitIndex = index & 7;

         if(on) Data[byteIndex] |= (byte)(1 << bitIndex);
         else Data[byteIndex] &= (byte)~(1 << bitIndex);
      }

      public GlyphBitmap Clone()
          => new GlyphBitmap(Size, (byte[])Data.Clone());

      private void ValidateXY(int x, int y)
      {
         if(x < 0 || x >= Size.Width) throw new ArgumentOutOfRangeException(nameof(x));
         if(y < 0 || y >= Size.Height) throw new ArgumentOutOfRangeException(nameof(y));
      }
   }
}