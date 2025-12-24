using Core.Models;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storage.FileSystem
{
   public static class GlyphHexCodecV1
   {
      public static async Task SaveAsync(string path, Glyph glyph, CancellationToken ct)
      {
         Directory.CreateDirectory(Path.GetDirectoryName(path)!);

         // Store as hex text, row-major, MSB = left-most pixel.
         // For width=8 this becomes 1 byte per row (height bytes total).
         var hex = Encode(glyph.Bitmap, glyph.Size);
         await File.WriteAllTextAsync(path, hex, Encoding.ASCII, ct).ConfigureAwait(false);
      }

      public static async Task<Glyph> LoadAsync(string path, GlyphId id, GridSize expectedSize, CancellationToken ct)
      {
         var text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
         var bitmap = Decode(text, expectedSize);
         return new Glyph(id, expectedSize, bitmap);
      }

      public static string Encode(GlyphBitmap bitmap, GridSize size)
      {
         if(bitmap == null) throw new ArgumentNullException(nameof(bitmap));
         if(size.Width != bitmap.Size.Width || size.Height != bitmap.Size.Height)
            throw new ArgumentException("Bitmap size mismatch.", nameof(size));

         int bytesPerRow = (size.Width + 7) / 8;
         var sb = new StringBuilder(size.Height * bytesPerRow * 2);

         for(int y = 0; y < size.Height; y++)
         {
            for(int bx = 0; bx < bytesPerRow; bx++)
            {
               byte value = 0;
               for(int bit = 0; bit < 8; bit++)
               {
                  int x = (bx * 8) + bit;
                  if(x >= size.Width)
                     break;

                  if(bitmap.GetPixel(x, y))
                  {
                     // MSB is left-most pixel.
                     value |= (byte)(1 << (7 - bit));
                  }
               }

               sb.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }
         }

         return sb.ToString();
      }

      public static GlyphBitmap Decode(string text, GridSize size)
      {
         if(text == null) throw new ArgumentNullException(nameof(text));

         // Allow whitespace/newlines for readability, but canonical save is no whitespace.
         var cleaned = new StringBuilder(text.Length);
         foreach(var ch in text)
         {
            if(char.IsWhiteSpace(ch))
               continue;
            cleaned.Append(ch);
         }

         string hex = cleaned.ToString();

         int bytesPerRow = (size.Width + 7) / 8;
         int expectedBytes = size.Height * bytesPerRow;
         int expectedHexChars = expectedBytes * 2;

         if(hex.Length != expectedHexChars)
            throw new InvalidDataException($"Invalid hex glyph length. Expected {expectedHexChars} hex chars for size {size}, got {hex.Length}.");

         var data = new byte[GlyphBitmap.GetByteLength(size)];
         var bmp = new GlyphBitmap(size, data);

         int i = 0;
         for(int y = 0; y < size.Height; y++)
         {
            for(int bx = 0; bx < bytesPerRow; bx++)
            {
               byte value = byte.Parse(hex.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
               i += 2;

               for(int bit = 0; bit < 8; bit++)
               {
                  int x = (bx * 8) + bit;
                  if(x >= size.Width)
                     break;

                  bool on = (value & (1 << (7 - bit))) != 0;
                  bmp.SetPixel(x, y, on);
               }
            }
         }

         return bmp;
      }
   }
}
