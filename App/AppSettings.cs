using Storage.Abstractions.Models;
using System.Collections.Generic;

namespace Tools
{
   public sealed class AppSettings
   {
      public List<string> SymbolsRoots { get; set; } = new List<string>();
      public int ActiveSymbolsRootIndex { get; set; } = 0;

      public string LastPackId { get; set; } = "8x16";
      public FontStyleId LastStyle { get; set; } = new FontStyleId("_base_");
      public string LastText { get; set; } = "Hello BOS 👋\r\nAfrowave Glyphs!";

      public int TerminalWidth { get; set; } = 40;
      public int TerminalHeight { get; set; } = 12;

      public int PixelScale { get; set; } = 2;
      public bool ShowGrid { get; set; } = true;
      public bool HighlightFallback { get; set; } = true;

      public string GetActiveSymbolsRoot()
      {
         if(SymbolsRoots == null || SymbolsRoots.Count == 0)
            return AppPaths.DefaultSymbolsRoot();

         if(ActiveSymbolsRootIndex < 0 || ActiveSymbolsRootIndex >= SymbolsRoots.Count)
            return SymbolsRoots[0];

         return SymbolsRoots[ActiveSymbolsRootIndex];
      }

      public void EnsureDefaults()
      {
         if(SymbolsRoots == null) SymbolsRoots = new List<string>();
         if(SymbolsRoots.Count == 0)
            SymbolsRoots.Add(AppPaths.DefaultSymbolsRoot());

                  if(ActiveSymbolsRootIndex < 0) ActiveSymbolsRootIndex = 0;
                  if(ActiveSymbolsRootIndex >= SymbolsRoots.Count) ActiveSymbolsRootIndex = 0;

                  if(string.IsNullOrWhiteSpace(LastPackId)) LastPackId = "8x16";
                  if(string.IsNullOrWhiteSpace(LastStyle.Name)) LastStyle = new FontStyleId("_base_");
               }
            }
         }
