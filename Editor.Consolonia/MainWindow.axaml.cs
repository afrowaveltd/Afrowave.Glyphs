using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Core.Models;
using Core.Naming;
using Editor.Avalonia.Services;
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

   public MainWindow()
   {
      InitializeComponent();
   }

   public void SetTerminalCache(TerminalGlyphCache cache)
   {
      _currentCache = cache;

      _applyRuntime = (vm, c) =>
      {
         _currentCache = c;
         // Controls removed from XAML - simplified TUI version
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
      if(DataContext is not MainViewModel hostVm)
         return;

      if(_workspace == null)
         return;

      var rasterizer = new FontGlyphRasterizer();
      var importService = new FontImportService(hostVm.Repository, rasterizer);

      var wizVm = new FontImportWizardViewModel(importService)
      {
         PackId = hostVm.SelectedPackId,
         Style = hostVm.SelectedStyle,
         GlyphSize = hostVm.SelectedGlyphSize
      };

      wizVm.PickFontFileHandler = async title =>
      {
         var picker = new FilePickerWindow();
         var startDir = FilePickerWindow.GetDefaultFontsDirectory();
            return await picker.PickAsync(this, title, startDir, ".ttf;.otf").ConfigureAwait(true);
         };

         GlyphEditorViewModel? editorVm = null;
         wizVm.ApplyBitmapToEditorHandler = async bmp =>
         {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
               if(bmp == null)
                  return;

               // IMPORTANT: Check if bitmap size matches current glyph size
               var currentSize = hostVm.SelectedGlyphSize;

               if(bmp != null && !bmp.Size.Equals(currentSize))
               {
                  // Bitmap has wrong size - log warning
                  System.Diagnostics.Debug.WriteLine($"WARNING: Import bitmap size {bmp.Size} doesn't match expected size {currentSize}");
               }

               // Replace edited bitmap for currently selected glyph id
               var id = GlyphId.FromUnicode(wizVm.CurrentCodePoint);
               hostVm.SelectedGlyph = id;
               hostVm.EditedBitmap = bmp;
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
         win.Initialize(wizVm);
         await win.ShowDialog(this).ConfigureAwait(true);

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

      var picker = new FilePickerWindow();
      // Start at current workspace root or current dir
      var start = _workspace.CurrentWorkspaceRoot;
      if(string.IsNullOrEmpty(start) || !System.IO.Directory.Exists(start))
         start = System.Environment.CurrentDirectory;

      var picked = await picker.PickAsync(this, "Select workspace folder", start, "", allowFolderSelection: true).ConfigureAwait(true);

      if(string.IsNullOrEmpty(picked))
         return;

      var root = picked;
      await _workspace.AddWorkspaceRootAsync(root).ConfigureAwait(true);
      await _workspace.EnsureSymbolsStructureAsync(root).ConfigureAwait(true);

      await ReinitializeForWorkspaceAsync(root).ConfigureAwait(true);

      Title = $"Consolonia - {root}";
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

      // Simple prompt for style name (TUI doesn't have fancy dialogs)
      // We'll create a simple input window
      var inputWin = new Window
      {
         Title = "New Style",
         Width = 60,
         Height = 10
      };

      var stack = new StackPanel { Spacing = 1, Margin = new Thickness(2) };
      stack.Children.Add(new TextBlock { Text = "Style name (e.g. arabic, emojis):" });

      var input = new TextBox { Watermark = "style_name" };
      stack.Children.Add(input);

      var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1, Margin = new Thickness(0, 1, 0, 0) };
      var okBtn = new Button { Content = "Create", Width = 10 };
      var cancelBtn = new Button { Content = "Cancel", Width = 10 };
      buttons.Children.Add(okBtn);
      buttons.Children.Add(cancelBtn);
      stack.Children.Add(buttons);

      inputWin.Content = stack;

      string? styleName = null;
      okBtn.Click += (s, e) => { styleName = input.Text?.Trim(); inputWin.Close(); };
      cancelBtn.Click += (s, e) => { inputWin.Close(); };

      await inputWin.ShowDialog(this).ConfigureAwait(true);

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
   }