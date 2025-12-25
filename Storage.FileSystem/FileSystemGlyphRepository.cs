using Core.Models;
using Core.Naming;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storage.FileSystem
{
   public sealed class FileSystemGlyphRepository : IGlyphRepository
   {
      private readonly PathMapping _paths;
      private readonly IGlyphNaming _naming;

      public FileSystemGlyphRepository(FileSystemOptions options, IGlyphNaming naming)
      {
         _naming = naming ?? throw new ArgumentNullException(nameof(naming));
         _paths = new PathMapping(options ?? throw new ArgumentNullException(nameof(options)), _naming);
      }

      public Task<bool> ExistsAsync(string packId, FontStyleId style, GlyphId id, CancellationToken cancellationToken)
      {
         var path = _paths.GetGlyphFilePath(packId, style, id);
         return Task.FromResult(File.Exists(path));
      }

      public async Task<Glyph> LoadAsync(string packId, FontStyleId style, GlyphId id, CancellationToken cancellationToken)
      {
         var path = _paths.GetGlyphFilePath(packId, style, id);

         // Preferred format: plain hex string in the glyph file.
         // Backward compatibility: if the file is legacy binary (AFWG), fall back to GlyphFileCodecV1.
         try
         {
            var size = ParsePackId(packId);
            return await GlyphHexCodecV1.LoadAsync(path, id, size, cancellationToken).ConfigureAwait(false);
         }
         catch(InvalidDataException)
         {
            // Silently fall back to legacy codec
            try
            {
               return await GlyphFileCodecV1.LoadAsync(path, id, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
               // If both fail, return empty glyph
               var size = ParsePackId(packId);
               return new Glyph(id, size, new GlyphBitmap(size, new byte[GlyphBitmap.GetByteLength(size)]));
            }
         }
         catch
         {
            // For any other error, try legacy codec
            try
            {
               return await GlyphFileCodecV1.LoadAsync(path, id, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
               // Return empty glyph as fallback
               var size = ParsePackId(packId);
               return new Glyph(id, size, new GlyphBitmap(size, new byte[GlyphBitmap.GetByteLength(size)]));
            }
         }
      }

      public async Task SaveAsync(string packId, FontStyleId style, Glyph glyph, CancellationToken cancellationToken)
      {
         if(glyph == null) throw new ArgumentNullException(nameof(glyph));

         var path = _paths.GetGlyphFilePath(packId, style, glyph.Id);
         await GlyphFileCodecV1.SaveAsync(path, glyph, cancellationToken).ConfigureAwait(false);
      }

      public Task DeleteAsync(string packId, FontStyleId style, GlyphId id, CancellationToken cancellationToken)
      {
         var path = _paths.GetGlyphFilePath(packId, style, id);
         if(File.Exists(path))
            File.Delete(path);

         return Task.CompletedTask;
      }

      public Task<IReadOnlyList<GlyphInfo>> ListGlyphsAsync(string packId, FontStyleId style, CancellationToken cancellationToken)
      {
         var folder = _paths.GetStyleFolder(packId, style);
         var list = new List<GlyphInfo>();

         if(!Directory.Exists(folder))
            return Task.FromResult((IReadOnlyList<GlyphInfo>)list);

         foreach(var file in Directory.EnumerateFiles(folder, "*" + DefaultGlyphNaming.Extension, SearchOption.TopDirectoryOnly))
         {
            var name = Path.GetFileName(file);
            if(_naming.TryParseFileName(name, out GlyphId id))
               list.Add(new GlyphInfo(id, exists: true));
         }

         return Task.FromResult((IReadOnlyList<GlyphInfo>)list);
      }

      private static GridSize ParsePackId(string packId)
      {
         // Expected format "WxH" e.g. "8x16".
         packId = (packId ?? string.Empty).Trim();
         var parts = packId.Split('x', 'X');
         if(parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
            return new GridSize(w, h);

         return new GridSize(8, 16);
      }
   }
}