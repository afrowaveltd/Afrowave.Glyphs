using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Core.Models;
using Core.Naming;
using Tools.Services;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using Storage.FileSystem;
using Tools;
using Tools.FontImport;

namespace Editor.Consolonia;

public partial class MainWindow : Window
{
   private EditorWorkspaceService? _workspace;
   private Action<MainViewModel, TerminalGlyphCache>? _applyRuntime;
   private TerminalGlyphCache? _currentCache;

   // View management
   private UserControl? _currentView;
   private ContentControl? _contentHost;

   public MainWindow()
   {
      InitializeComponent();

      // Get content host
      _contentHost = this.FindControl<ContentControl>("ContentHost");
   }

   public void ShowMainView()
   {
      // Main view is already the default content in XAML
      // No need to change anything
   }

   public async Task ShowFontImportWizard(FontImportWizardViewModel wizVm)
   {
      // TODO: Consolonia - Font import wizard (reserved for future TUI development)
      // For now, show placeholder message
      System.Diagnostics.Debug.WriteLine("[Consolonia] Font import wizard not implemented in TUI version");
      await Task.CompletedTask;

      /* OLD CODE - Views removed
      var importView = new FontImportWizardView
      {
         DataContext = wizVm
      };

      if(_contentHost != null)
      {
         _contentHost.Content = importView;
      }

      wizVm.CloseHandler = async () =>
      {
         if(_contentHost != null)
         {
            _contentHost.Content = this.FindControl<Grid>("MainViewContent");
         }

         if(DataContext is MainViewModel vm)
         {
            await vm.ReloadAsync();
         }
      };
      */
   }

   public async Task ShowGlyphEditor(GlyphEditorViewModel editorVm)
   {
      // TODO: Consolonia - Glyph editor (reserved for future TUI development)
      System.Diagnostics.Debug.WriteLine("[Consolonia] Glyph editor not implemented in TUI version");
         await Task.CompletedTask;

         /* OLD CODE - Views removed
         var editorView = new GlyphEditorView
         {
            DataContext = editorVm
         };

         if(_contentHost != null)
         {
            _contentHost.Content = editorView;
         }

         var tcs = new TaskCompletionSource<bool>();

         editorView.AcceptClicked += (s, e) =>
         {
            if(_contentHost != null)
            {
               _contentHost.Content = this.FindControl<Grid>("MainViewContent");
            }
            tcs.SetResult(true);
         };

         editorView.CancelClicked += (s, e) =>
         {
            if(_contentHost != null)
            {
               _contentHost.Content = this.FindControl<Grid>("MainViewContent");
            }
            tcs.SetResult(false);
         };

         await tcs.Task;
         */
      }

   public void SetTerminalCache(TerminalGlyphCache cache)
   {
      _currentCache = cache;

      _applyRuntime = (vm, c) =>
      {
         _currentCache = c;

         // Setup text change listener for ASCII preview
         vm.PropertyChanged += OnViewModelPropertyChanged;

         // Initial render
         UpdateAsciiPreview(vm);
      };

      if(DataContext is MainViewModel vm)
         _applyRuntime(vm, cache);
   }

   private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
   {
      if(e.PropertyName == nameof(MainViewModel.Text) || 
         e.PropertyName == nameof(MainViewModel.SelectedPackId) ||
         e.PropertyName == nameof(MainViewModel.SelectedStyle) ||
         e.PropertyName == nameof(MainViewModel.SelectedGlyphSize))
      {
         if(sender is MainViewModel vm)
            UpdateAsciiPreview(vm);
      }
   }

   private async void UpdateAsciiPreview(MainViewModel vm)
   {
      if(_currentCache == null) return;

      // Ensure we're on UI thread
      if(!Dispatcher.UIThread.CheckAccess())
      {
         await Dispatcher.UIThread.InvokeAsync(() => UpdateAsciiPreview(vm));
         return;
      }

      var previewBlock = this.FindControl<TextBlock>("PreviewAscii");
      if(previewBlock == null) return;

      var text = vm.Text ?? "";
      if(string.IsNullOrEmpty(text))
      {
         previewBlock.Text = "(type text above to see ASCII preview)";
         return;
      }

      // Calculate how many characters fit based on window width and glyph size
      // Assume window width ~85 chars for preview area (120 total - 35 left panel)
      var windowWidth = (int)Width - 40; // Subtract left panel + margins
      var charWidth = vm.SelectedGlyphSize.Width + 1; // +1 for space between chars
      var maxChars = Math.Max(1, Math.Min(text.Length, windowWidth / charWidth));

      var limitedText = text.Length > maxChars ? text.Substring(0, maxChars) : text;

      var ascii = await RenderToAsciiArtAsync(limitedText, vm);
      previewBlock.Text = ascii + (text.Length > maxChars ? $"\n... ({text.Length - maxChars} more chars)" : "");
   }

   private async Task<string> RenderToAsciiArtAsync(string text, MainViewModel vm)
   {
      var sb = new System.Text.StringBuilder();
      var glyphSize = vm.SelectedGlyphSize;

      try
      {
         // Render each character side-by-side
         for(int row = 0; row < glyphSize.Height; row++)
         {
            for(int charIndex = 0; charIndex < text.Length; charIndex++)
            {
               var ch = text[charIndex];
               var glyphId = GlyphId.FromUnicode((int)ch);

               var bitmap = await _currentCache.GetBitmapAsync(
                  vm.SelectedPackId, 
                  vm.SelectedStyle, 
                  glyphId, 
                  glyphSize, 
                  vm.FallbackGlyph, 
                  System.Threading.CancellationToken.None);

               // Render this row of the glyph
               for(int col = 0; col < glyphSize.Width; col++)
               {
                  var pixel = bitmap.GetPixel(col, row);
                  sb.Append(pixel ? "█" : "·");  // Full block or dot
               }

               sb.Append(" ");  // Space between characters
            }
            sb.AppendLine();
         }
      }
      catch
      {
         return "(preview error - check pack/style)";
      }

      return sb.ToString();
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
               vm.CreateStyleHandler = CreateNewStyleAsync;
               vm.GlyphSavedHandler = OnGlyphSavedAsync;
               vm.GlyphBitmapReplacedHandler = OnGlyphBitmapReplacedAsync;
               vm.CloneGlyphHandler = CloneGlyphAsync;
               vm.UnicodeRangeWizardHandler = UnicodeRangeWizardAsync;
            }
         }

      private Task OnGlyphBitmapReplacedAsync(GlyphId id, GlyphBitmap bitmap)
      {
         // Simplified TUI - no glyph preview control
         return Task.CompletedTask;
      }

                     private async Task ImportFontAsync()
                  {
                     if(DataContext is not MainViewModel hostVm) return;
                     if(_workspace == null) return;

                     // Consolonia SOLUTION: Use inline view instead of new Window!
                     var rasterizer = new FontGlyphRasterizer();
                     var importService = new FontImportService(hostVm.Repository, rasterizer);

                     var wizVm = new FontImportWizardViewModel(importService)
                     {
                        PackId = hostVm.SelectedPackId,
                        Style = hostVm.SelectedStyle,
                        GlyphSize = hostVm.SelectedGlyphSize,
                        FontPath = FilePickerWindow.GetDefaultFontsDirectory()
                     };

                     // Setup handlers
                     wizVm.ApplyBitmapToEditorHandler = async bmp =>
                     {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                           if(bmp == null) return;

                           var currentSize = hostVm.SelectedGlyphSize;
                           if(bmp != null && !bmp.Size.Equals(currentSize))
                           {
                              System.Diagnostics.Debug.WriteLine($"WARNING: Import bitmap size {bmp.Size} doesn't match expected size {currentSize}");
                           }

                           var id = GlyphId.FromUnicode(wizVm.CurrentCodePoint);
                           hostVm.SelectedGlyph = id;
                           hostVm.EditedBitmap = bmp;
                        });
                     };

                     wizVm.ReadBitmapFromEditorHandler = () => Task.FromResult(hostVm.EditedBitmap);

                     wizVm.GlyphSavedHandler = async () =>
                     {
                        if(_currentCache != null)
                        {
                           await Dispatcher.UIThread.InvokeAsync(async () =>
                           {
                              _currentCache.Clear();
                              await hostVm.RenderAsync();
                           });
                        }
                     };

                     // Show wizard as inline view (no new Window!)
                     await ShowFontImportWizard(wizVm);
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

      // Use simple prompt - no Window to avoid Consolonia crashes
      var start = _workspace.CurrentWorkspaceRoot;
      if(string.IsNullOrEmpty(start) || !System.IO.Directory.Exists(start))
         start = System.Environment.CurrentDirectory;

      // Show input in status bar or use existing dialog pattern
      // For now, just use a very simple approach: text file or config
      // TODO: Implement proper dialog without Window class

      // Temporary workaround: Use current directory
      var root = start;

      await _workspace.AddWorkspaceRootAsync(root).ConfigureAwait(true);
      await _workspace.EnsureSymbolsStructureAsync(root).ConfigureAwait(true);

      await ReinitializeForWorkspaceAsync(root).ConfigureAwait(true);

      Title = $"Consolonia - {root} (Workspace reload - use Settings to change)";
   }

   private async Task CreateSymbolsAsync()
   {
      if(_workspace == null) return;
      var root = _workspace.CurrentWorkspaceRoot;
      await _workspace.EnsureSymbolsStructureAsync(root).ConfigureAwait(true);
      Title = $"Consolonia - {root} (Symbols ready)";
   }

   private async Task CreateNewStyleAsync()
   {
      if(DataContext is not MainViewModel vm) return;
      if(_workspace == null) return;

      // Use inline dialog instead of Window
      var styleName = await InlineDialogHelper.ShowTextInputAsync(
         this,
         "New Style",
         "Style name (e.g. arabic, emojis):",
         watermark: "style_name");

      if(string.IsNullOrWhiteSpace(styleName)) return;

      // Create style directory in current pack
      var symbolsRoot = _workspace.ResolveSymbolsFolder(_workspace.CurrentWorkspaceRoot);
      var stylePath = System.IO.Path.Combine(symbolsRoot, vm.SelectedPackId, styleName);

      if(System.IO.Directory.Exists(stylePath))
      {
         Title = $"Consolonia - Style '{styleName}' already exists!";
         return;
      }

      // Create style directory
      System.IO.Directory.CreateDirectory(stylePath);

      // Reload styles
      await vm.ReloadAsync().ConfigureAwait(true);

      // Select new style
      vm.SelectedStyle = new FontStyleId(styleName);

      Title = $"Consolonia - Created style '{styleName}'";
   }

   private async Task ReinitializeForWorkspaceAsync(string workspaceRoot)
   {
      if(_workspace == null) return;

      var symbolsRoot = _workspace.ResolveSymbolsFolder(workspaceRoot);

      IGlyphNaming naming = new DefaultGlyphNaming();
      IGlyphRepository repo = new FileSystemGlyphRepository(new FileSystemOptions(symbolsRoot), naming);
      IFontPackProvider packs = new FileSystemFontPackProvider(new FileSystemOptions(symbolsRoot));

      var old = DataContext as MainViewModel;

      // Save old values
      var oldText = old?.Text ?? string.Empty;
      var oldPackId = old?.SelectedPackId ?? "8x16";
      var oldStyle = old?.SelectedStyle ?? new FontStyleId("_base_");

      var vm = new MainViewModel(packs, repo)
      {
         Text = oldText
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

         // Load packs and styles FIRST
         await vm.ReloadAsync().ConfigureAwait(false);

         // THEN set selected values
         if(vm.PackIds.Contains(oldPackId))
            vm.SelectedPackId = oldPackId;
         else if(vm.PackIds.Count > 0)
            vm.SelectedPackId = vm.PackIds[0];

         if(vm.Styles.Contains(oldStyle))
            vm.SelectedStyle = oldStyle;
         else if(vm.Styles.Count > 0)
            vm.SelectedStyle = vm.Styles[0];
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

         // Consolonia CRITICAL: Close this window BEFORE creating editor
         var app = Application.Current;
         if(app?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

         var savedVm = vm;
         var savedWorkspace = _workspace;
         var savedCache = _currentCache;

         Close(); // Close main window FIRST!

         await Dispatcher.UIThread.InvokeAsync(async () =>
         {
            var editor = new GlyphEditorWindow(); // Only NOW create editor!
            desktop.MainWindow = editor;

            var accepted = await editor.EditAsync(editorVm, null);

            // Restore main window
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
               var newMain = new MainWindow();
               newMain.SetWorkspace(savedWorkspace!);
               newMain.DataContext = savedVm;
               newMain.SetTerminalCache(savedCache!);

               if(accepted)
               {
                  savedVm.EditedBitmap = editorVm.Bitmap;
               }

               desktop.MainWindow = newMain;
               newMain.Show();
            });
         });
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

      private void OnExit(object sender, RoutedEventArgs e)
      {
         var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
         lifetime!.Shutdown();
      }

      private async void OnCloneGlyph(object? sender, RoutedEventArgs e)
      {
         if(DataContext is not MainViewModel vm) return;

         // Use inline dialog instead of Window
         var targetText = await InlineDialogHelper.ShowTextInputAsync(
            this,
            "Clone Glyph",
            $"Clone '{vm.SelectedGlyph}' to:",
            watermark: "U+0041 or A or internal_name");

         if(string.IsNullOrWhiteSpace(targetText)) return;

         // Parse target ID
         GlyphId? targetId = null;
         var text = targetText.Trim();

         if(text.Length == 1)
            targetId = GlyphId.FromUnicode((int)text[0]);
         else if(text.StartsWith("U+", StringComparison.OrdinalIgnoreCase) && int.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int cp))
            targetId = GlyphId.FromUnicode(cp);
         else
            targetId = GlyphId.FromInternal(text);

         if(!targetId.HasValue) return;

         // Clone current glyph to target ID
         if(vm.EditedBitmap != null)
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

         // Consolonia CRITICAL: Close main window BEFORE creating wizard
         var app = Application.Current;
         if(app?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

         var savedVm = vm;
         var savedWorkspace = _workspace;
         var savedCache = _currentCache;

         Close(); // Close FIRST!

         await Dispatcher.UIThread.InvokeAsync(async () =>
         {
            var win = new UnicodeRangeWizardWindow(); // Only NOW create wizard!
            win.Initialize(wizVm);
            desktop.MainWindow = win;

            await win.ShowDialog(null);

            // Restore main window
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
               var newMain = new MainWindow();
               newMain.SetWorkspace(savedWorkspace!);
               newMain.DataContext = savedVm;
               newMain.SetTerminalCache(savedCache!);

               desktop.MainWindow = newMain;
               newMain.Show();

               await newMain.OnGlyphSavedAsync();
            });
         });
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
   }