using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Core.Models;
using Core.Naming;
using Editor.Avalonia.Services;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using Storage.FileSystem;
using System;
using System.Threading.Tasks;
using Tools;
using Tools.FontImport;

namespace Editor.Avalonia;

public partial class MainWindow : Window
{
   private EditorWorkspaceService? _workspace;
   private Action<MainViewModel, TerminalGlyphCache>? _applyRuntime;
   private TerminalGlyphCache? _currentCache;

   public MainWindow()
   {
      InitializeComponent();
   }

   private void InitializeComponent()
   {
      AvaloniaXamlLoader.Load(this);
   }

   public void SetTerminalCache(TerminalGlyphCache cache)
   {
      _currentCache = cache;

      _applyRuntime = (vm, c) =>
      {
         _currentCache = c;

         var terminal = this.FindControl<TerminalPreviewControl>("Terminal")!;
         terminal.SetCache(c);

         // propagate selection to VM (once)
         terminal.CellClicked -= (x, y) => {}; // Remove old handler
         terminal.CellClicked += (x, y) => 
         {
            if(DataContext is MainViewModel vm2)
            {
               // In Avalonia version, clicking terminal doesn't change selection
               // vm2.SelectedGlyph = /* calculate from x,y */;
            }
         };

            var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview")!;
            preview.SetCache(c);

            preview.GlyphEdited -= PreviewOnGlyphEdited;
            preview.GlyphEdited += PreviewOnGlyphEdited;

            preview.UndoRedoStateChanged -= UpdateUndoRedoButtons;
            preview.UndoRedoStateChanged += UpdateUndoRedoButtons;

            preview.CurrentToolChanged -= UpdateToolButtons;
            preview.CurrentToolChanged += UpdateToolButtons;

            UpdateUndoRedoButtons(); // Initial state
            UpdateToolButtons(Editor.Avalonia.Controls.DrawTool.Pencil); // Initial tool
         };

      if(DataContext is MainViewModel vm)
         _applyRuntime(vm, cache);
   }

   private void TerminalOnGlyphClicked(GlyphId glyphId)
   {
      if(DataContext is MainViewModel vm)
      {
         vm.SelectedGlyph = glyphId;
      }
   }

   private void PreviewOnGlyphEdited(GlyphId id, GlyphBitmap bitmap)
   {
      if(DataContext is MainViewModel vm)
         vm.EditedBitmap = bitmap;
   }

   public void SetWorkspace(EditorWorkspaceService workspace)
   {
      _workspace = workspace;

         if(DataContext is MainViewModel vm)
         {
            vm.OpenWorkspaceHandler = OpenWorkspaceAsync;
            vm.CreateSymbolsHandler = CreateSymbolsAsync;
            vm.ImportFontHandler = ImportFontAsync;
            vm.CreatePackHandler = CreateNewPackAsync;
            vm.CreateStyleHandler = CreateNewStyleAsync;
            vm.GlyphSavedHandler = OnGlyphSavedAsync;
            vm.GlyphBitmapReplacedHandler = OnGlyphBitmapReplacedAsync;
            vm.CloneGlyphHandler = CloneGlyphAsync;
            vm.UnicodeRangeWizardHandler = UnicodeRangeWizardAsync;
         }
      }

   private async Task OnGlyphBitmapReplacedAsync(GlyphId id, GlyphBitmap bitmap)
   {
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview")!;
         preview.ReplaceEditedBitmap(id, bitmap);
      });
   }

   private async Task ImportFontAsync()
   {
      if(DataContext is not MainViewModel hostVm)
         return;

      if(_workspace == null)
         return;

      // File picker for font
      var topLevel = TopLevel.GetTopLevel(this);
      if(topLevel == null) return;

      // Get system fonts directory as starting location
      var fontsDir = AppPaths.GetSystemFontsDirectory();

      // Validate access to fonts directory before setting it as start location
      bool canAccessFonts = false;
      if(!string.IsNullOrEmpty(fontsDir))
      {
         try
         {
            // Test access by enumerating directory
            if(System.IO.Directory.Exists(fontsDir))
            {
               _ = System.IO.Directory.GetFiles(fontsDir, "*.ttf", System.IO.SearchOption.TopDirectoryOnly);
               canAccessFonts = true;
            }
         }
         catch
         {
            // No access - will use fallback
            canAccessFonts = false;
         }
      }

      var options = new global::Avalonia.Platform.Storage.FilePickerOpenOptions
      {
         Title = "Select Font File",
         AllowMultiple = false,
         FileTypeFilter = new[]
         {
            new global::Avalonia.Platform.Storage.FilePickerFileType("TrueType Fonts") { Patterns = new[] { "*.ttf", "*.otf" } },
            new global::Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
         }
      };

      // Set suggested start location to fonts directory (if accessible) or Documents
      string startLocation = canAccessFonts ? fontsDir : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

      if(!string.IsNullOrEmpty(startLocation))
      {
         try
         {
            var startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(startLocation));
            if(startFolder != null)
               options.SuggestedStartLocation = startFolder;
         }
         catch
         {
            // Ignore errors - picker will use default location
         }
      }

      // Open file picker with error handling
      System.Collections.Generic.IReadOnlyList<IStorageFile> files;
      try
      {
         files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
      }
      catch(UnauthorizedAccessException)
      {
         Title = "Afrowave Glyph Editor - Access denied to selected location";
         return;
      }
      catch(System.IO.IOException ex)
      {
         Title = $"Afrowave Glyph Editor - I/O Error: {ex.Message}";
         System.Diagnostics.Debug.WriteLine($"[ImportFont] I/O Error: {ex}");
         return;
      }
      catch(Exception ex)
      {
         Title = $"Afrowave Glyph Editor - Error opening file picker: {ex.Message}";
         System.Diagnostics.Debug.WriteLine($"[ImportFont] Error: {ex}");
         return;
      }

      if(files == null || files.Count == 0) return;

      var fontPath = files[0].Path.LocalPath;

      var rasterizer = new FontGlyphRasterizer();
      var importService = new FontImportService(hostVm.Repository, rasterizer);

      var wizVm = new FontImportWizardViewModel(importService)
      {
         PackId = hostVm.SelectedPackId,
         Style = hostVm.SelectedStyle,
         GlyphSize = hostVm.SelectedGlyphSize,  // Use current size from main VM
         FontPath = fontPath  // IMPORTANT: Set the selected font path
      };

      wizVm.PickFontFileHandler = async title =>
      {
         return fontPath; // Already picked
      };

      GlyphEditorViewModel? editorVm = null;
      wizVm.ApplyBitmapToEditorHandler = async bmp =>
      {
         await Dispatcher.UIThread.InvokeAsync(() =>
         {
            if(bmp == null)
               return;

            // IMPORTANT: Always use the current GlyphSize from the main ViewModel
            var currentSize = hostVm.SelectedGlyphSize;

            // Check if bitmap size matches current glyph size
            if(bmp != null && !bmp.Size.Equals(currentSize))
            {
               // Bitmap has wrong size - this shouldn't happen, but log it
               System.Diagnostics.Debug.WriteLine($"WARNING: Import bitmap size {bmp.Size} doesn't match expected size {currentSize}");
               // Keep the bitmap anyway - it's what was rasterized
            }

            // Replace edited bitmap for currently selected glyph id
            var id = GlyphId.FromUnicode(wizVm.CurrentCodePoint);
            hostVm.SelectedGlyph = id;
            hostVm.EditedBitmap = bmp;

            // Update the preview control to show the new glyph
            var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
            if(preview != null && bmp != null)
            {
               preview.ReplaceEditedBitmap(id, bmp);
            }
         });
      };

         wizVm.ReadBitmapFromEditorHandler = () => Task.FromResult(hostVm.EditedBitmap);

         wizVm.GlyphSavedHandler = async () =>
         {
            // Clear cache and refresh preview when glyph is saved
            if(_currentCache != null)
            {
               await Dispatcher.UIThread.InvokeAsync(async () =>
               {
                  _currentCache.Clear();
                  await hostVm.RenderAsync();
               });
            }
         };

         var win = new FontImportWizardWindow();
         win.RequestedThemeVariant = ActualThemeVariant; // Apply current theme
         win.Initialize(wizVm);
         win.Show(this);
         await Task.CompletedTask;

         // Refresh after import
         await hostVm.ReloadAsync().ConfigureAwait(true);
      }

   private async Task OnGlyphSavedAsync()
   {
      await Dispatcher.UIThread.InvokeAsync(async () =>
      {
         _currentCache?.Clear();

         if(DataContext is MainViewModel vm)
            await vm.RenderAsync();
      });
   }

   private async Task OpenWorkspaceAsync()
   {
      if(_workspace == null) return;

      var picked = await StorageProvider.OpenFolderPickerAsync(new global::Avalonia.Platform.Storage.FolderPickerOpenOptions
      {
         Title = "Select workspace folder",
         AllowMultiple = false
      }).ConfigureAwait(true);

      if(picked == null || picked.Count == 0)
         return;

      var root = picked[0].Path.LocalPath;
      await _workspace.AddWorkspaceRootAsync(root).ConfigureAwait(true);
      await _workspace.EnsureSymbolsStructureAsync(root).ConfigureAwait(true);

      await ReinitializeForWorkspaceAsync(root).ConfigureAwait(true);

      Title = $"Afrowave Glyph Editor - {root}";
   }

   private async Task CreateSymbolsAsync()
   {
      if(_workspace == null) return;
      var root = _workspace.CurrentWorkspaceRoot;
      await _workspace.EnsureSymbolsStructureAsync(root).ConfigureAwait(true);
      Title = $"Afrowave Glyph Editor - {root} (Symbols ready)";
   }

   private async Task CreateNewStyleAsync()
   {
      if(DataContext is not MainViewModel vm) return;
      if(_workspace == null) return;

      var dialog = new Window
      {
         Title = "Create New Style",
         Width = 400,
         Height = 200,
         WindowStartupLocation = WindowStartupLocation.CenterOwner,
         CanResize = false
      };

      var stack = new StackPanel { Margin = new Thickness(20) };
      stack.Children.Add(new TextBlock { Text = "Enter style name (e.g., 'arabic', 'cyrillic', 'emojis'):", Margin = new Thickness(0, 0, 0, 10) });

      var input = new TextBox { Watermark = "StyleName", Margin = new Thickness(0, 0, 0, 20) };
      stack.Children.Add(input);

      var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 10 };
      var createBtn = new Button { Content = "Create", MinWidth = 100, Padding = new Thickness(10, 5) };
      var cancelBtn = new Button { Content = "Cancel", MinWidth = 100, Padding = new Thickness(10, 5) };
      buttons.Children.Add(createBtn);
      buttons.Children.Add(cancelBtn);
      stack.Children.Add(buttons);

      dialog.Content = stack;

      string? styleName = null;
      createBtn.Click += (s, e) => { styleName = input.Text?.Trim(); dialog.Close(); };
      cancelBtn.Click += (s, e) => { dialog.Close(); };

      await dialog.ShowDialog(this);

      if(string.IsNullOrWhiteSpace(styleName)) return;

      var symbolsRoot = _workspace.ResolveSymbolsFolder(_workspace.CurrentWorkspaceRoot);
      var stylePath = System.IO.Path.Combine(symbolsRoot, vm.SelectedPackId, styleName);

      if(System.IO.Directory.Exists(stylePath))
      {
         Title = $"Afrowave Glyph Editor - Style '{styleName}' already exists!";
         return;
      }

      System.IO.Directory.CreateDirectory(stylePath);
      await vm.ReloadAsync();
      vm.SelectedStyle = new FontStyleId(styleName);
      Title = $"Afrowave Glyph Editor - Created style '{styleName}' in pack '{vm.SelectedPackId}'";
   }

   private async Task ReinitializeForWorkspaceAsync(string workspaceRoot)
   {
      if(_workspace == null) return;

      var symbolsRoot = _workspace.ResolveSymbolsFolder(workspaceRoot);

      IGlyphNaming naming = new DefaultGlyphNaming();
      IGlyphRepository repo = new FileSystemGlyphRepository(new FileSystemOptions(symbolsRoot), naming);
      IFontPackProvider packs = new FileSystemFontPackProvider(new FileSystemOptions(symbolsRoot));

      var old = DataContext as MainViewModel;

      var vm = new MainViewModel(packs, repo)
      {
         Text = old?.Text ?? string.Empty,
         SelectedPackId = old?.SelectedPackId ?? "8x16",
         SelectedStyle = old?.SelectedStyle ?? new FontStyleId("_base_")
      };

      vm.OpenWorkspaceHandler = OpenWorkspaceAsync;
      vm.CreateSymbolsHandler = CreateSymbolsAsync;
      vm.ImportFontHandler = ImportFontAsync;
      vm.CreateStyleHandler = CreateNewStyleAsync;
      vm.GlyphSavedHandler = OnGlyphSavedAsync;
      vm.GlyphBitmapReplacedHandler = OnGlyphBitmapReplacedAsync;
      vm.CloneGlyphHandler = CloneGlyphAsync;
      vm.UnicodeRangeWizardHandler = UnicodeRangeWizardAsync;

      var cache = new TerminalGlyphCache(repo);

      await Dispatcher.UIThread.InvokeAsync(async () =>
      {
         DataContext = vm;
         SetTerminalCache(cache);
         await vm.ReloadAsync().ConfigureAwait(false);
      });
   }

   private async void OnEditGlyph(object? sender, RoutedEventArgs e)
   {
      if(DataContext is not MainViewModel vm) return;

      // Ensure we have a bitmap to edit with correct size
      GlyphBitmap? bmp = vm.EditedBitmap;

      // If bitmap doesn't exist or has wrong size, load or create new one
      if(bmp == null || !bmp.Size.Equals(vm.SelectedGlyphSize))
      {
         // Load from repo
         if(await vm.Repository.ExistsAsync(vm.SelectedPackId, vm.SelectedStyle, vm.SelectedGlyph, System.Threading.CancellationToken.None))
         {
            var glyph = await vm.Repository.LoadAsync(vm.SelectedPackId, vm.SelectedStyle, vm.SelectedGlyph, System.Threading.CancellationToken.None);
            bmp = glyph.Bitmap.Clone();

            // If loaded bitmap has wrong size, create new one (shouldn't happen, but be safe)
            if(!bmp.Size.Equals(vm.SelectedGlyphSize))
            {
               bmp = new GlyphBitmap(vm.SelectedGlyphSize, new byte[GlyphBitmap.GetByteLength(vm.SelectedGlyphSize)]);
            }
         }
         else
         {
            // Create new empty with correct size
            bmp = new GlyphBitmap(vm.SelectedGlyphSize, new byte[GlyphBitmap.GetByteLength(vm.SelectedGlyphSize)]);
         }

         vm.EditedBitmap = bmp;
      }

      var editorVm = new GlyphEditorViewModel(bmp.Clone());
      var editor = new GlyphEditorWindow();
      var accepted = await editor.EditAsync(editorVm, this).ConfigureAwait(true);

      if(accepted)
      {
         vm.EditedBitmap = editorVm.Bitmap;
         // Optionally save immediately? Or let user click Save?
         // The UI has a Save button, so we just update the VM state.
         // But maybe we should save to be user friendly.
         // For now, just update VM.
      }
   }

   private void OnSetGlyphId(object? sender, RoutedEventArgs e)
   {
      if(DataContext is not MainViewModel vm) return;
      var input = this.FindControl<TextBox>("GlyphIdInput")?.Text;
      if(string.IsNullOrWhiteSpace(input)) return;

      if(input.Length == 1)
         vm.SelectedGlyph = GlyphId.FromUnicode((int)input[0]);
      else if(input.StartsWith("U+", StringComparison.OrdinalIgnoreCase) && int.TryParse(input.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int cp))
         vm.SelectedGlyph = GlyphId.FromUnicode(cp);
      else
         vm.SelectedGlyph = GlyphId.FromInternal(input);
   }

   private async void OnBrowseGlyphs(object? sender, RoutedEventArgs e)
   {
      if(DataContext is not MainViewModel vm) return;

      var browserVm = new GlyphBrowserViewModel(vm.Repository, vm.SelectedPackId, vm.SelectedStyle, vm.SelectedGlyphSize);

      GlyphBrowserWindow? browser = null;

      // Handler for editing glyph from browser
      browserVm.EditGlyphHandler = async (glyphId, bitmap) =>
      {
         await Dispatcher.UIThread.InvokeAsync(() =>
         {
            // Set selected glyph and bitmap in main VM
            vm.SelectedGlyph = glyphId;
            vm.EditedBitmap = bitmap.Clone();

            // Update preview control
            var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
            if(preview != null)
            {
               preview.ReplaceEditedBitmap(glyphId, bitmap.Clone());
            }
         });
      };

         // Handler to close browser window after Edit
         browserVm.CloseWindowHandler = () =>
         {
            // Don't close - just bring main window to front
            this.Activate();
         };

         browser = new GlyphBrowserWindow();
         browser.RequestedThemeVariant = ActualThemeVariant; // Apply current theme
         browser.Initialize(browserVm);
         browser.Show(); // Modeless - doesn't block main window
      }

   private void OnExit(object sender, RoutedEventArgs e)
   {
      var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
      lifetime!.Shutdown();
   }

   private async void OnCloneGlyph(object? sender, RoutedEventArgs e)
   {
      if(DataContext is not MainViewModel vm) return;

      var dialog = new Window
      {
         Title = "Clone Glyph",
         Width = 40,
         Height = 10,
         WindowStartupLocation = WindowStartupLocation.CenterOwner
      };

      var stack = new StackPanel { Margin = new global::Avalonia.Thickness(1) };
      stack.Children.Add(new TextBlock { Text = $"Clone '{vm.SelectedGlyph}' to:" });

      var input = new TextBox { Margin = new global::Avalonia.Thickness(0, 1, 0, 1) };
      stack.Children.Add(input);

      var buttons = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 1 };
      var okBtn = new Button { Content = "OK" };
      var cancelBtn = new Button { Content = "Cancel" };
      buttons.Children.Add(okBtn);
      buttons.Children.Add(cancelBtn);
      stack.Children.Add(buttons);

      dialog.Content = stack;

      GlyphId? targetId = null;
      okBtn.Click += (s, args) =>
      {
         var text = input.Text?.Trim();
         if(string.IsNullOrEmpty(text))
         {
            dialog.Close();
            return;
         }

         if(text.Length == 1)
            targetId = GlyphId.FromUnicode((int)text[0]);
         else if(text.StartsWith("U+", StringComparison.OrdinalIgnoreCase) && int.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int cp))
            targetId = GlyphId.FromUnicode(cp);
         else
            targetId = GlyphId.FromInternal(text);

         dialog.Close();
      };

      cancelBtn.Click += (s, args) => dialog.Close();

      await dialog.ShowDialog(this);

      if(targetId == null) return;

      // Clone current glyph to target ID
      if(targetId.HasValue && vm.EditedBitmap != null)
      {
         var glyph = new Glyph(targetId.Value, vm.GlyphSize, vm.EditedBitmap.Clone());
         await vm.Repository.SaveAsync(vm.SelectedPackId, vm.SelectedStyle, glyph, System.Threading.CancellationToken.None);
         vm.SelectedGlyph = targetId.Value;
         await OnGlyphSavedAsync();
      }
   }

   private async void OnUnicodeRange(object? sender, RoutedEventArgs e)
   {
      if(DataContext is not MainViewModel vm) return;

      var wizVm = new UnicodeRangeWizardViewModel(vm.Repository)
      {
         PackId = vm.SelectedPackId,
         Style = vm.SelectedStyle,
         GlyphSize = vm.SelectedGlyphSize,
         StartCodePoint = 32,
         EndCodePoint = 126
      };

      var win = new UnicodeRangeWizardWindow();
      win.RequestedThemeVariant = ActualThemeVariant; // Apply current theme
      win.Initialize(wizVm);

      if(_currentCache != null)
      {
         // Simplified TUI - no glyph preview control in wizard
      }

      await win.ShowDialog(this);
      await OnGlyphSavedAsync();
   }

   private async Task CloneGlyphAsync()
   {
      // This is called from command binding, but we handle it via event
      await Task.CompletedTask;
   }

      private async Task UnicodeRangeWizardAsync()
      {
         // This is called from command binding, but we handle it via event
         await Task.CompletedTask;
      }

      private async Task CreateNewPackAsync()
      {
         if(DataContext is not MainViewModel vm) return;
         if(_workspace == null) return;

         var dialog = new Window
         {
            Title = "Create New Pack",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
         };

         var stack = new StackPanel { Margin = new Thickness(20) };
         stack.Children.Add(new TextBlock { Text = "Enter pack size (e.g., '8x16', '16x32', '32x64'):", Margin = new Thickness(0, 0, 0, 10) });

         var input = new TextBox { Watermark = "8x16", Margin = new Thickness(0, 0, 0, 20) };
         stack.Children.Add(input);

         var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 10 };
         var createBtn = new Button { Content = "Create", MinWidth = 100, Padding = new Thickness(10, 5) };
         var cancelBtn = new Button { Content = "Cancel", MinWidth = 100, Padding = new Thickness(10, 5) };
         buttons.Children.Add(createBtn);
         buttons.Children.Add(cancelBtn);
         stack.Children.Add(buttons);

         dialog.Content = stack;

         string? packSize = null;
         createBtn.Click += (s, e) => { packSize = input.Text?.Trim(); dialog.Close(); };
         cancelBtn.Click += (s, e) => { dialog.Close(); };

         await dialog.ShowDialog(this);

         if(string.IsNullOrWhiteSpace(packSize)) return;

         // Validate format (WxH)
         var parts = packSize.Split('x', 'X');
         if(parts.Length != 2 || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h) || w <= 0 || h <= 0)
         {
            Title = "Afrowave Glyph Editor - Invalid pack size format! Use WxH (e.g., 8x16)";
            return;
         }

         var symbolsRoot = _workspace.ResolveSymbolsFolder(_workspace.CurrentWorkspaceRoot);
         var packPath = System.IO.Path.Combine(symbolsRoot, packSize);
         var defaultStylePath = System.IO.Path.Combine(packPath, "_base_");

         if(System.IO.Directory.Exists(packPath))
         {
            Title = $"Afrowave Glyph Editor - Pack '{packSize}' already exists!";
            return;
         }

         // Create pack folder + default _base_ style
         System.IO.Directory.CreateDirectory(defaultStylePath);

         // Create _missing.glyph
         var gsize = new GridSize(w, h);
         var missingPath = System.IO.Path.Combine(defaultStylePath, "_missing.glyph");
         var emptyBitmap = new GlyphBitmap(gsize, new byte[GlyphBitmap.GetByteLength(gsize)]);
         var content = GlyphHexCodecV1.Encode(emptyBitmap, gsize);
         await System.IO.File.WriteAllTextAsync(missingPath, content);

         await vm.ReloadAsync();
         vm.SelectedPackId = packSize;
         vm.SelectedStyle = new FontStyleId("_base_");
            Title = $"Afrowave Glyph Editor - Created pack '{packSize}'";
         }

         private void OnToggleTheme(object? sender, RoutedEventArgs e)
         {
            var currentTheme = ActualThemeVariant;
            if(currentTheme == global::Avalonia.Styling.ThemeVariant.Dark)
            {
               // Switch to Light theme
               RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;

               // Load Light theme resources
               if(Application.Current is App app)
               {
                  app.SetTheme("Light");
               }

               // Update button
               if(sender is Button btn)
               {
                  btn.Content = "☀️";
                  btn.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Light gray for visibility
               }
            }
            else
            {
               // Switch to Dark theme
               RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;

               // Load Dark theme resources
               if(Application.Current is App app)
               {
                  app.SetTheme("Dark");
               }

               // Update button
               if(sender is Button btn)
               {
                  btn.Content = "🌙";
                  btn.Background = new SolidColorBrush(Color.FromRgb(37, 37, 37)); // Dark gray
               }
            }
         }

      private void OnSelectPencil(object? sender, RoutedEventArgs e)
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         if(preview != null)
            preview.CurrentTool = Editor.Avalonia.Controls.DrawTool.Pencil;
      }

      private void OnSelectLine(object? sender, RoutedEventArgs e)
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         if(preview != null)
            preview.CurrentTool = Editor.Avalonia.Controls.DrawTool.Line;
      }

      private void OnSelectRectangle(object? sender, RoutedEventArgs e)
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         if(preview != null)
            preview.CurrentTool = Editor.Avalonia.Controls.DrawTool.Rectangle;
      }

      private void OnSelectCircle(object? sender, RoutedEventArgs e)
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         if(preview != null)
            preview.CurrentTool = Editor.Avalonia.Controls.DrawTool.Circle;
      }

      private void OnSelectFill(object? sender, RoutedEventArgs e)
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         if(preview != null)
            preview.CurrentTool = Editor.Avalonia.Controls.DrawTool.Fill;
      }

      private void OnUndo(object? sender, RoutedEventArgs e)
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         preview?.Undo();
      }

      private void OnRedo(object? sender, RoutedEventArgs e)
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         preview?.Redo();
      }

      private void UpdateUndoRedoButtons()
      {
         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview");
         var undoBtn = this.FindControl<Button>("UndoButton");
         var redoBtn = this.FindControl<Button>("RedoButton");

         if(preview != null && undoBtn != null && redoBtn != null)
         {
            undoBtn.IsEnabled = preview.CanUndo;
            redoBtn.IsEnabled = preview.CanRedo;
         }
      }

      private void UpdateToolButtons(Editor.Avalonia.Controls.DrawTool currentTool)
      {
         var pencilBtn = this.FindControl<Button>("PencilButton");
         var lineBtn = this.FindControl<Button>("LineButton");
         var rectBtn = this.FindControl<Button>("RectangleButton");
         var circleBtn = this.FindControl<Button>("CircleButton");
         var fillBtn = this.FindControl<Button>("FillButton");

         // Reset all buttons to default style
         var defaultBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Transparent
         var activeBrush = new SolidColorBrush(Color.FromArgb(80, 100, 150, 255)); // Light blue highlight

         if(pencilBtn != null) pencilBtn.Background = defaultBrush;
         if(lineBtn != null) lineBtn.Background = defaultBrush;
         if(rectBtn != null) rectBtn.Background = defaultBrush;
         if(circleBtn != null) circleBtn.Background = defaultBrush;
         if(fillBtn != null) fillBtn.Background = defaultBrush;

         // Highlight active tool
         switch(currentTool)
         {
            case Editor.Avalonia.Controls.DrawTool.Pencil:
               if(pencilBtn != null) pencilBtn.Background = activeBrush;
               break;
            case Editor.Avalonia.Controls.DrawTool.Line:
               if(lineBtn != null) lineBtn.Background = activeBrush;
               break;
            case Editor.Avalonia.Controls.DrawTool.Rectangle:
               if(rectBtn != null) rectBtn.Background = activeBrush;
               break;
            case Editor.Avalonia.Controls.DrawTool.Circle:
               if(circleBtn != null) circleBtn.Background = activeBrush;
               break;
            case Editor.Avalonia.Controls.DrawTool.Fill:
               if(fillBtn != null) fillBtn.Background = activeBrush;
               break;
         }
      }

      public async Task<bool> ShowMessageAsync(string title, string message, string yesButton, string noButton)
      {
         var dialog = new Window
         {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
         };

         var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
         stack.Children.Add(new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });

         var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 10 };
         var yesBtn = new Button { Content = yesButton, MinWidth = 100 };
         var noBtn = new Button { Content = noButton, MinWidth = 100 };
         buttons.Children.Add(yesBtn);
         buttons.Children.Add(noBtn);
         stack.Children.Add(buttons);

         dialog.Content = stack;

         bool result = false;
         yesBtn.Click += (s, ev) => { result = true; dialog.Close(); };
         noBtn.Click += (s, ev) => { result = false; dialog.Close(); };

         await dialog.ShowDialog(this);
         return result;
      }
   }