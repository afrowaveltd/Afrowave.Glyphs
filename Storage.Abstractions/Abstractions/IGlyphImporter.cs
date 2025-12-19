using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storage.Abstractions.Abstractions
{
   public interface IGlyphImporter
   {
      string Name { get; }

      // Např. "ttf", "otf"
      IReadOnlyList<string> SupportedFormats { get; }

      Task<IReadOnlyList<Glyph>> ImportAsync(
          string sourcePath,
          GridSize gridSize,
          CancellationToken cancellationToken);
   }
}