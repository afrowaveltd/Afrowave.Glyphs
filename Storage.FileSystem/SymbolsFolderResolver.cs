using System;
using System.IO;

namespace Storage.FileSystem
{
   public static class SymbolsFolderResolver
   {
      public const string SymbolsFolderName = "Symbols";

      public static string ResolveSymbolsRoot(string workspaceRoot, bool createIfMissing = true)
      {
         if(string.IsNullOrWhiteSpace(workspaceRoot))
            throw new ArgumentException("workspaceRoot cannot be empty.", nameof(workspaceRoot));

         workspaceRoot = workspaceRoot.Trim();

         if(!Directory.Exists(workspaceRoot))
         {
            if(!createIfMissing)
               throw new DirectoryNotFoundException($"Workspace root not found: {workspaceRoot}");

            Directory.CreateDirectory(workspaceRoot);
         }

         foreach(var dir in Directory.EnumerateDirectories(workspaceRoot, "*", SearchOption.TopDirectoryOnly))
         {
            var name = Path.GetFileName(dir);
            if(string.Equals(name, SymbolsFolderName, StringComparison.OrdinalIgnoreCase))
               return dir;
         }

         var symbols = Path.Combine(workspaceRoot, SymbolsFolderName);
         if(createIfMissing)
            Directory.CreateDirectory(symbols);
         return symbols;
      }
   }
}
