using Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storage.FileSystem
{
   internal static class GlyphFileCodecV1
   {
      private static readonly byte[] Magic = { (byte)'A', (byte)'F', (byte)'W', (byte)'G' };

      public static async Task SaveAsync(string path, Glyph glyph, CancellationToken ct)
      {
         Directory.CreateDirectory(Path.GetDirectoryName(path)!);

         using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
         // Header
         await fs.WriteAsync(Magic, 0, Magic.Length, ct).ConfigureAwait(false);
         await fs.WriteAsync(new[] { (byte)1 }, 0, 1, ct).ConfigureAwait(false); // version
         await fs.WriteAsync(new[] { checked((byte)glyph.Size.Width) }, 0, 1, ct).ConfigureAwait(false);
         await fs.WriteAsync(new[] { checked((byte)glyph.Size.Height) }, 0, 1, ct).ConfigureAwait(false);
         await fs.WriteAsync(new[] { (byte)0 }, 0, 1, ct).ConfigureAwait(false); // flags = 0

         // Data
         await fs.WriteAsync(glyph.Bitmap.Data, 0, glyph.Bitmap.Data.Length, ct).ConfigureAwait(false);
         await fs.FlushAsync(ct).ConfigureAwait(false);
      }

      public static async Task<Glyph> LoadAsync(string path, GlyphId id, CancellationToken ct)
      {
         using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
         var header = new byte[8]; // 4 magic + 1 ver + 1 w + 1 h + 1 flags
         var read = await ReadExactlyAsync(fs, header, 0, header.Length, ct).ConfigureAwait(false);
         if(read != header.Length) throw new EndOfStreamException("Invalid glyph file header.");

         // Magic
         for(int i = 0; i < 4; i++)
            if(header[i] != Magic[i]) throw new InvalidDataException("Invalid glyph magic.");

         var version = header[4];
         if(version != 1) throw new InvalidDataException("Unsupported glyph version: " + version);

         var width = header[5];
         var height = header[6];
         // flags = header[7] (unused)

         var size = new GridSize(width, height);
         var dataLen = GlyphBitmap.GetByteLength(size);

         var data = new byte[dataLen];
         var dataRead = await ReadExactlyAsync(fs, data, 0, data.Length, ct).ConfigureAwait(false);
         if(dataRead != data.Length) throw new EndOfStreamException("Invalid glyph file data.");

         var bitmap = new GlyphBitmap(size, data);
         return new Glyph(id, size, bitmap);
      }

      private static async Task<int> ReadExactlyAsync(Stream s, byte[] buffer, int offset, int count, CancellationToken ct)
      {
         int total = 0;
         while(total < count)
         {
            int n = await s.ReadAsync(buffer, offset + total, count - total, ct).ConfigureAwait(false);
            if(n == 0) break;
            total += n;
         }
         return total;
      }
   }
}