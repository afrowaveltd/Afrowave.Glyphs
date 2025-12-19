using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Storage.Abstractions.Abstractions
{
   public interface IFontPackProvider
   {
      Task<IReadOnlyList<FontPackDescriptor>> ListPacksAsync(CancellationToken cancellationToken);

      Task<IReadOnlyList<FontStyleId>> ListStylesAsync(string packId, CancellationToken cancellationToken);

      // Volitelně: ověření existence / vytvoření packu a stylu
      Task<bool> PackExistsAsync(string packId, CancellationToken cancellationToken);

      Task<bool> StyleExistsAsync(string packId, FontStyleId style, CancellationToken cancellationToken);
   }
}