using Storage.FileSystem;
using System;
using System.IO;
using System.Threading.Tasks;
using Core.Models;
using Storage.Abstractions.Models;

namespace Tools.Services
{
   /// <summary>
   /// Shared service for managing editor workspace (used by both Avalonia and Consolonia)
   /// </summary>
   public sealed class EditorWorkspaceService
   {
      private readonly SettingsService _settings;

      public EditorWorkspaceService(SettingsService settings)
      {
         _settings = settings ?? throw new ArgumentNullException(nameof(settings));
      }

      public string CurrentWorkspaceRoot => _settings.Current.GetActiveSymbolsRoot();

      public static string GetDefaultWorkspaceRoot()
      {
         // Default: Documents/Afrowave/GlyphEditor
         var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
         var defaultPath = Path.Combine(documents, "Afrowave", "GlyphEditor");

         // Create if it doesn't exist
         if(!Directory.Exists(defaultPath))
            Directory.CreateDirectory(defaultPath);

         return defaultPath;
      }

      public string ResolveSymbolsFolder(string workspaceRoot)
         => SymbolsFolderResolver.ResolveSymbolsRoot(workspaceRoot, createIfMissing: true);

      public async Task AddWorkspaceRootAsync(string workspaceRoot)
      {
         await _settings.AddSymbolsRootAsync(workspaceRoot).ConfigureAwait(false);
      }

      public async Task EnsureSymbolsStructureAsync(string workspaceRoot)
      {
         var symbolsRoot = SymbolsFolderResolver.ResolveSymbolsRoot(workspaceRoot, createIfMissing: true);

         // Minimal recommended folders for MVP.
         var packId = _settings.Current.LastPackId;
         if(string.IsNullOrWhiteSpace(packId))
            packId = "8x16";

         var baseStyle = new FontStyleId("_base_");
         Directory.CreateDirectory(Path.Combine(symbolsRoot, packId, baseStyle.Name));

         // Create `_missing.glyph` if it does not exist.
         var missingPath = Path.Combine(symbolsRoot, packId, baseStyle.Name, "_missing.glyph");
         if(!File.Exists(missingPath))
         {
            var size = ParsePackId(packId);
            var bmp = new GlyphBitmap(size, new byte[GlyphBitmap.GetByteLength(size)]);

            // Simple '?' like mark for 8x16-ish: border + dot. This is intentionally minimal.
            // Works for any size >= 5x7; for smaller sizes it degenerates to a few pixels.
            int w = size.Width;
            int h = size.Height;

            void P(int x, int y)
            {
               if(x < 0 || y < 0 || x >= w || y >= h) return;
               bmp.SetPixel(x, y, true);
            }

            // Top curve
            for(int x = 1; x < Math.Min(w - 1, 6); x++) P(x, 1);
            P(Math.Min(w - 2, 6), 2);
            P(Math.Min(w - 2, 6), 3);
            P(Math.Min(w - 3, 5), 4);
            P(Math.Min(w - 4, 4), 5);

            // Dot
            P(Math.Min(w / 2, w - 2), h - 2);

            var glyph = new Glyph(GlyphId.FromInternal("missing"), size, bmp);
            await GlyphHexCodecV1.SaveAsync(missingPath, glyph, default).ConfigureAwait(false);
         }

         return;
      }

      private static GridSize ParsePackId(string packId)
      {
         packId = (packId ?? string.Empty).Trim();
         var parts = packId.Split('x', 'X');
         if(parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
            return new GridSize(w, h);

         return new GridSize(8, 16);
      }

      public Task SaveSettingsAsync() => _settings.SaveAsync();
   }
}
