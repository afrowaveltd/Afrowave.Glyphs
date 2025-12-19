using Core.Models;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storage.Abstractions.Abstractions
{
   public interface IGlyphRepository
   {
      Task<IReadOnlyList<GlyphInfo>> ListGlyphsAsync(
          string packId,
          FontStyleId style,
          CancellationToken cancellationToken);

      Task<bool> ExistsAsync(
          string packId,
          FontStyleId style,
          GlyphId id,
          CancellationToken cancellationToken);

      Task<Glyph> LoadAsync(
          string packId,
          FontStyleId style,
          GlyphId id,
          CancellationToken cancellationToken);

      Task SaveAsync(
          string packId,
          FontStyleId style,
          Glyph glyph,
          CancellationToken cancellationToken);

      Task DeleteAsync(
          string packId,
          FontStyleId style,
          GlyphId id,
          CancellationToken cancellationToken);
   }
}