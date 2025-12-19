using Core.Models;
using Core.Naming;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Storage.FileSystem
{
   internal sealed class PathMapping
   {
      private readonly FileSystemOptions _options;
      private readonly IGlyphNaming _naming;

      public PathMapping(FileSystemOptions options, IGlyphNaming naming)
      {
         _options = options ?? throw new ArgumentNullException(nameof(options));
         _naming = naming ?? throw new ArgumentNullException(nameof(naming));
      }

      public string GetPackFolder(GridSize gridSize, string packId)
      {
         // packId is the "<WxH>" folder for BOS usage (e.g. "8x16") or a named pack in future.
         // For now, we assume packId == $"{W}x{H}" as discussed.
         return Path.Combine(_options.SymbolsRootPath, packId);
      }

      public string GetStyleFolder(string packId, FontStyleId style)
          => Path.Combine(GetPackFolderFromPackId(packId), style.Name);

      private string GetPackFolderFromPackId(string packId)
      {
         packId = (packId ?? string.Empty).Trim();
         if(packId.Length == 0) throw new ArgumentException("packId cannot be empty.", nameof(packId));
         return Path.Combine(_options.SymbolsRootPath, packId);
      }

      public string GetGlyphFilePath(string packId, FontStyleId style, GlyphId id)
          => Path.Combine(GetStyleFolder(packId, style), _naming.ToFileName(id));
   }
}