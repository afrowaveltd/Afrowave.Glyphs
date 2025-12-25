using Core.Models;
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
   public sealed class FileSystemFontPackProvider : IFontPackProvider
   {
      private readonly FileSystemOptions _options;

      public FileSystemFontPackProvider(FileSystemOptions options)
      {
         _options = options ?? throw new ArgumentNullException(nameof(options));
      }

      public Task<IReadOnlyList<FontPackDescriptor>> ListPacksAsync(CancellationToken cancellationToken)
      {
         var list = new List<FontPackDescriptor>();

         if(!Directory.Exists(_options.SymbolsRootPath))
            return Task.FromResult((IReadOnlyList<FontPackDescriptor>)list);

         foreach(var dir in Directory.EnumerateDirectories(_options.SymbolsRootPath))
         {
            var packId = Path.GetFileName(dir);

            // BOS-first: packId like "8x16" / "16x16"
            // IMPORTANT: Ignore folders that start with underscore (these are styles, not packs!)
            if(packId.StartsWith("_"))
            {
               System.Diagnostics.Debug.WriteLine($"[PACK PROVIDER] Ignoring style folder: {packId}");
               continue;
            }

            if(TryParseGridSize(packId, out GridSize size))
               list.Add(new FontPackDescriptor(packId, packId, size));
            else
               list.Add(new FontPackDescriptor(packId, packId, new GridSize(8, 16))); // fallback, editor can refine later
         }

         return Task.FromResult((IReadOnlyList<FontPackDescriptor>)list);
      }

      public Task<IReadOnlyList<FontStyleId>> ListStylesAsync(string packId, CancellationToken cancellationToken)
      {
         var list = new List<FontStyleId>();
         var packFolder = Path.Combine(_options.SymbolsRootPath, packId);

         if(!Directory.Exists(packFolder))
            return Task.FromResult((IReadOnlyList<FontStyleId>)list);

         foreach(var dir in Directory.EnumerateDirectories(packFolder))
         {
            list.Add(new FontStyleId(Path.GetFileName(dir)));
         }

         return Task.FromResult((IReadOnlyList<FontStyleId>)list);
      }

      public Task<bool> PackExistsAsync(string packId, CancellationToken cancellationToken)
          => Task.FromResult(Directory.Exists(Path.Combine(_options.SymbolsRootPath, packId)));

      public Task<bool> StyleExistsAsync(string packId, FontStyleId style, CancellationToken cancellationToken)
          => Task.FromResult(Directory.Exists(Path.Combine(_options.SymbolsRootPath, packId, style.Name)));

      private static bool TryParseGridSize(string text, out GridSize size)
      {
         size = default;
         if(string.IsNullOrWhiteSpace(text)) return false;

         var parts = text.ToLowerInvariant().Split('x');
         if(parts.Length != 2) return false;

         if(!int.TryParse(parts[0], out int w)) return false;
         if(!int.TryParse(parts[1], out int h)) return false;

         if(w <= 0 || h <= 0) return false;
         size = new GridSize(w, h);
         return true;
      }
   }
}