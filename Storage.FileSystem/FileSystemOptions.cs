using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.FileSystem
{
   public sealed class FileSystemOptions
   {
      public string SymbolsRootPath { get; }

      public FileSystemOptions(string symbolsRootPath)
      {
         symbolsRootPath = (symbolsRootPath ?? string.Empty).Trim();
         if(symbolsRootPath.Length == 0) throw new ArgumentException("SymbolsRootPath cannot be empty.", nameof(symbolsRootPath));
         SymbolsRootPath = symbolsRootPath;
      }
   }
}