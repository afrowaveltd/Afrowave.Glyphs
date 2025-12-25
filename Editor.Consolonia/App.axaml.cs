using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Core.Naming;
using Tools.Services;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using Storage.FileSystem;
using System;
using System.Threading.Tasks;
using Tools;

namespace Editor.Consolonia;

public partial class App : Application
{
   public override void Initialize()
   {
      AvaloniaXamlLoader.Load(this);
   }

   public override void OnFrameworkInitializationCompleted()
   {
      if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
         // In Consolonia we don't have a splash screen, so we just start init
         // and set the main window when ready.
         // However, we need a window to be set immediately for the app to start?
         // Consolonia usually needs a window. Let's create a temporary one or just MainWindow.
         
         // We'll create MainWindow but initialize it asynchronously.
         var window = new MainWindow();
         desktop.MainWindow = window;

         _ = InitAsync(desktop, window);
      }

      base.OnFrameworkInitializationCompleted();
   }

   private async Task InitAsync(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
   {
      try
      {
         // 1) Load settings
         var store = new JsonAppSettingsStore();
         var settingsService = new SettingsService(store);
         await settingsService.InitializeAsync();

         var workspaceService = new EditorWorkspaceService(settingsService);

         var settings = settingsService.Current;
         string workspaceRoot = settings.GetActiveSymbolsRoot();

         // If no workspace set, use default: Documents/Afrowave/GlyphEditor
         if(string.IsNullOrWhiteSpace(workspaceRoot))
         {
            workspaceRoot = EditorWorkspaceService.GetDefaultWorkspaceRoot();
            await workspaceService.AddWorkspaceRootAsync(workspaceRoot);
            await workspaceService.EnsureSymbolsStructureAsync(workspaceRoot);
         }

         string symbolsRoot = SymbolsFolderResolver.ResolveSymbolsRoot(workspaceRoot, createIfMissing: true);

         // 2) Create FS storage services (repo + pack provider)
         IGlyphRepository repo = CreateRepository(symbolsRoot);
         IFontPackProvider packs = CreatePackProvider(symbolsRoot);

         // 3) ViewModel
         var vm = new MainViewModel(packs, repo);

         // 4) Create cache
         var cache = new TerminalGlyphCache(repo);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
               // 5) Setup Window first (set DataContext before setting properties that trigger bindings)
               window.DataContext = vm;
               window.SetWorkspace(workspaceService);
               window.SetTerminalCache(cache);

               // Transfer settings to VM AFTER DataContext is set
               vm.Text = settings.LastText;

               // Set default pack if empty
               string packId = settings.LastPackId;
               if(string.IsNullOrWhiteSpace(packId))
                  packId = "8x16";

               // DON'T set SelectedPackId yet - let ReloadAsync() populate PackIds first

               // Load packs + styles + render
               await vm.ReloadAsync();

               System.Diagnostics.Debug.WriteLine($"[APP INIT] After ReloadAsync: PackIds.Count = {vm.PackIds.Count}");
               foreach(var p in vm.PackIds)
                  System.Diagnostics.Debug.WriteLine($"[APP INIT] PackId: {p}");

               // NOW set SelectedPackId AFTER PackIds is populated
               if(vm.PackIds.Contains(packId))
               {
                  System.Diagnostics.Debug.WriteLine($"[APP INIT] Setting SelectedPackId to {packId} (found in collection)");
                  vm.SelectedPackId = packId;
               }
               else if(vm.PackIds.Count > 0)
               {
                  System.Diagnostics.Debug.WriteLine($"[APP INIT] Setting SelectedPackId to {vm.PackIds[0]} (first in collection)");
                  vm.SelectedPackId = vm.PackIds[0];
               }
               else
               {
                  System.Diagnostics.Debug.WriteLine($"[APP INIT] No packs found! Setting fallback to {packId}");
                  vm.SelectedPackId = packId; // Fallback even if empty
               }

               // Set SelectedStyle AFTER Reload (so Styles collection is populated)
               var lastStyle = settings.LastStyle;
               if(string.IsNullOrWhiteSpace(lastStyle.Name))
                  lastStyle = new FontStyleId("_base_");

               if(vm.Styles.Contains(lastStyle))
                  vm.SelectedStyle = lastStyle;
               else if(vm.Styles.Count > 0)
                  vm.SelectedStyle = vm.Styles[0];
               else
                  vm.SelectedStyle = lastStyle; // Fallback even if empty

               // 6) Check if workspace is empty
               if(vm.PackIds.Count == 0)
               {
                  // In TUI, we can't show a nice dialog, so just set a message in title
                  window.Title = "Afrowave Glyph Editor - No packs found! Use 'Workspace' button to select folder.";
               }
            });
         }
      catch(Exception ex)
      {
         // Log error or show message box?
         // In TUI, maybe write to console or show a dialog if possible.
         System.Diagnostics.Debug.WriteLine($"Init Error: {ex}");
      }
   }

   private static IGlyphRepository CreateRepository(string symbolsRoot)
   {
      var options = new FileSystemOptions(symbolsRoot);
      IGlyphNaming naming = new DefaultGlyphNaming();
      return new FileSystemGlyphRepository(options, naming);
   }

   private static IFontPackProvider CreatePackProvider(string symbolsRoot)
   {
      var options = new FileSystemOptions(symbolsRoot);
      return new FileSystemFontPackProvider(options);
   }
}