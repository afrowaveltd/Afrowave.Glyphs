using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.Abstractions.Models
{
   public sealed class FontPackDescriptor
   {
      public string PackId { get; }
      public string DisplayName { get; }
      public GridSize GridSize { get; }

      public FontPackDescriptor(string packId, string displayName, GridSize gridSize)
      {
         packId = (packId ?? string.Empty).Trim();
         displayName = (displayName ?? string.Empty).Trim();

         if(packId.Length == 0) throw new ArgumentException("PackId cannot be empty.", nameof(packId));
         if(displayName.Length == 0) displayName = packId;

         PackId = packId;
         DisplayName = displayName;
         GridSize = gridSize;
      }

      public override string ToString() => DisplayName;
   }
}