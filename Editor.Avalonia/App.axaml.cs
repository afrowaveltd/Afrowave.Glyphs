using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Core.Naming;
using Tools.Services;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using Storage.FileSystem;
using System;
using System.IO;
using System.Threading.Tasks;
using Tools;


namespace Editor.Avalonia;

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
         // Show a splash screen to keep the app alive while initializing
         var splash = new Window
         {
            Content = new TextBlock
            {
               Text = "Loading...",
               HorizontalAlignment = HorizontalAlignment.Center,
               VerticalAlignment = VerticalAlignment.Center
            },
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SystemDecorations = SystemDecorations.BorderOnly,
            Title = "Loading"
         };

         desktop.MainWindow = splash;

         // Async init
         _ = InitAsync(desktop, splash);
      }

      base.OnFrameworkInitializationCompleted();
   }

   private async Task InitAsync(IClassicDesktopStyleApplicationLifetime desktop, Window splash)
   {
      try
      {
         // 1) Load settings
         var store = new JsonAppSettingsStore();
         var settingsService = new SettingsService(store);
         await settingsService.InitializeAsync();

         var workspaceService = new EditorWorkspaceService(settingsService);

         var settings = settingsService.Current;

         // IMPORTANT: SymbolsRoots should contain WORKSPACE ROOT (e.g., .../GlyphEditor),
         // NOT Symbols root (e.g., .../GlyphEditor/Symbols)!
         // GetActiveSymbolsRoot() is misleadingly named - it returns workspace root.
         string workspaceRoot = settings.GetActiveSymbolsRoot();

         // If no workspace set, use default: Documents/Afrowave/GlyphEditor
         if(string.IsNullOrWhiteSpace(workspaceRoot))
         {
            workspaceRoot = EditorWorkspaceService.GetDefaultWorkspaceRoot();
            System.Diagnostics.Debug.WriteLine($"[INIT] Creating default workspace: {workspaceRoot}");
            await workspaceService.AddWorkspaceRootAsync(workspaceRoot);
            await workspaceService.EnsureSymbolsStructureAsync(workspaceRoot);
            System.Diagnostics.Debug.WriteLine($"[INIT] Workspace structure created");
         }

         // VALIDATION: Ensure workspaceRoot doesn't end with "Symbols" (common bug)
         if(workspaceRoot.EndsWith("Symbols", StringComparison.OrdinalIgnoreCase) ||
            workspaceRoot.EndsWith("Symbols\\", StringComparison.OrdinalIgnoreCase) ||
            workspaceRoot.EndsWith("Symbols/", StringComparison.OrdinalIgnoreCase))
         {
            // Strip trailing "Symbols" to get true workspace root
            workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
            System.Diagnostics.Debug.WriteLine($"[INIT] Fixed workspaceRoot: {workspaceRoot}");

            // Update settings with corrected path
            await workspaceService.AddWorkspaceRootAsync(workspaceRoot);
         }

         string symbolsRoot = SymbolsFolderResolver.ResolveSymbolsRoot(workspaceRoot, createIfMissing: true);
         System.Diagnostics.Debug.WriteLine($"[INIT] Symbols root: {symbolsRoot}");
         System.Diagnostics.Debug.WriteLine($"[INIT] Symbols root exists: {Directory.Exists(symbolsRoot)}");

         if(Directory.Exists(symbolsRoot))
         {
            var dirs = Directory.GetDirectories(symbolsRoot);
            System.Diagnostics.Debug.WriteLine($"[INIT] Found {dirs.Length} pack directories:");
            foreach(var dir in dirs)
            {
               System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(dir)}");
            }
         }
         else
         {
            System.Diagnostics.Debug.WriteLine($"[INIT] WARNING: Symbols root does not exist!");
         }

         // 2) Create FS storage services (repo + pack provider)
         IGlyphRepository repo = CreateRepository(symbolsRoot);
         IFontPackProvider packs = CreatePackProvider(symbolsRoot);

         // 3) ViewModel (sdíletelný i s Consolonia)
         var vm = new MainViewModel(packs, repo);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
               // 4) Terminal cache pro Avalonia control
               var cache = new TerminalGlyphCache(repo);

               // 5) Window - set DataContext FIRST
               var window = new MainWindow();
               window.DataContext = vm;
               window.SetTerminalCache(cache);
               window.SetWorkspace(workspaceService);

               desktop.MainWindow = window;
               window.Show();
               splash.Close();

               // Transfer saved values from settings to VM AFTER DataContext is set
               vm.Text = settings.LastText;

               // Set default pack if empty
               string packId = settings.LastPackId;
               if(string.IsNullOrWhiteSpace(packId))
                  packId = "8x16";

               // DON'T set SelectedPackId yet - let ReloadAsync() populate PackIds first

               // Load packs + styles + render (on UI thread)
               await vm.ReloadAsync();

               // NOW set SelectedPackId AFTER PackIds is populated
               if(vm.PackIds.Contains(packId))
                  vm.SelectedPackId = packId;
               else if(vm.PackIds.Count > 0)
                  vm.SelectedPackId = vm.PackIds[0];
               else
                  vm.SelectedPackId = packId; // Fallback even if empty

               // Note: SelectedStyle is already set by ReloadAsync() → ReloadStylesAndRenderAsync()
               // Only override if user has a saved preference that differs from default
               var lastStyle = settings.LastStyle;
               if(!string.IsNullOrWhiteSpace(lastStyle.Name) && vm.Styles.Contains(lastStyle))
               {
                  // User has a saved style preference - use it
                  vm.SelectedStyle = lastStyle;
               }
               // else: keep the style selected by ReloadStylesAndRenderAsync() (should be _base_)

               // 6) Check if workspace is empty and prompt user
               if(vm.PackIds.Count == 0)
               {
                  var result = await window.ShowMessageAsync(
                     "No Glyphs Found",
                     "The workspace doesn't contain any glyph packs. Would you like to create a sample font pack?",
                     "Create Sample",
                     "Cancel");

                  if(result)
                  {
                     // Create sample 8x16 font
                     await SampleFontGenerator.CreateSampleFontAsync(repo, "8x16", new Core.Models.GridSize(8, 16), System.Threading.CancellationToken.None);

                     // Reload packs
                     await vm.ReloadAsync();

                     // NOW set selections again after ReloadAsync
                     if(vm.PackIds.Contains("8x16"))
                        vm.SelectedPackId = "8x16";
                     else if(vm.PackIds.Count > 0)
                        vm.SelectedPackId = vm.PackIds[0];

                     var baseStyle = new FontStyleId("_base_");
                     if(vm.Styles.Contains(baseStyle))
                        vm.SelectedStyle = baseStyle;
                     else if(vm.Styles.Count > 0)
                        vm.SelectedStyle = vm.Styles[0];

                     window.Title = "Afrowave Glyph Editor - Sample font created!";
                  }
               }
            });
         }
      catch(Exception ex)
      {
         await Dispatcher.UIThread.InvokeAsync(() =>
         {
            if(splash.Content is TextBlock tb)
            {
               tb.Text = $"Error: {ex.Message}";
               tb.Foreground = Brushes.Red;
            }
         });
      }
   }

   private static IGlyphRepository CreateRepository(string symbolsRoot)
   {
      var options = new FileSystemOptions(symbolsRoot);

      // Default naming: UTF / internal / custom (_afw_a apod.)
      IGlyphNaming naming = new DefaultGlyphNaming();

      return new FileSystemGlyphRepository(options, naming);
   }

      private static IFontPackProvider CreatePackProvider(string symbolsRoot)
      {
         var options = new FileSystemOptions(symbolsRoot);
         return new FileSystemFontPackProvider(options);
      }

      public void SetTheme(string themeName)
      {
         var themeFile = themeName == "Light" ? "/Themes/LightTheme.axaml" : "/Themes/DarkTheme.axaml";

         var newTheme = (IResourceProvider)AvaloniaXamlLoader.Load(new Uri($"avares://Editor.Avalonia{themeFile}"));

         if(Resources.MergedDictionaries.Count > 0)
            Resources.MergedDictionaries[0] = newTheme;
         else
            Resources.MergedDictionaries.Add(newTheme);
      }
   }