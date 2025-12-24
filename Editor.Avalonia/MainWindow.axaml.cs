using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Editor.Avalonia.Services;
using Core.Naming;
using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.FileSystem;
using Tools;
using Tools.FontImport;
using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Editor.Avalonia;

public partial class MainWindow : Window
{
   public MainWindow() => InitializeComponent();

   private EditorWorkspaceService? _workspace;
   private Action<MainViewModel, TerminalGlyphCache>? _applyRuntime;
   private TerminalGlyphCache? _currentCache;

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
         terminal.CellClicked -= TerminalOnCellClicked;
         terminal.CellClicked += TerminalOnCellClicked;

         var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview")!;
         preview.SetCache(c);

         preview.GlyphEdited -= PreviewOnGlyphEdited;
         preview.GlyphEdited += PreviewOnGlyphEdited;
      };

      if(DataContext is MainViewModel vm)
         _applyRuntime(vm, cache);

   }

   private void PreviewOnGlyphEdited(Core.Models.GlyphId id, Core.Models.GlyphBitmap bitmap)
   {
      if(DataContext is MainViewModel vm)
         vm.EditedBitmap = bitmap;
   }

   private System.Threading.Tasks.Task OnGlyphBitmapReplacedAsync(Core.Models.GlyphId id, Core.Models.GlyphBitmap bitmap)
   {
      var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview")!;
      preview.ReplaceEditedBitmap(id, bitmap);
      return Task.CompletedTask;
   }

   private void TerminalOnCellClicked(int x, int y)
   {
      if(DataContext is MainViewModel vm)
         vm.Text = $"Clicked cell: {x},{y}";
   }

   public void SetWorkspace(EditorWorkspaceService workspace)
   {
      _workspace = workspace;

      if(DataContext is MainViewModel vm)
      {
         vm.OpenWorkspaceHandler = OpenWorkspaceAsync;
         vm.CreateSymbolsHandler = CreateSymbolsAsync;
         vm.ImportFontHandler = ImportFontAsync;
         vm.GlyphSavedHandler = OnGlyphSavedAsync;
         vm.GlyphBitmapReplacedHandler = OnGlyphBitmapReplacedAsync;
      }
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

      var preview = this.FindControl<Editor.Avalonia.Controls.GlyphPreviewControl>("GlyphPreview")!;

      wizVm.ApplyBitmapToEditorHandler = async bmp =>
      {
         if(bmp == null)
            return;

         // Replace edited bitmap for currently selected glyph id
         var id = GlyphId.FromUnicode(wizVm.CurrentCodePoint);
         hostVm.SelectedGlyph = id;
         hostVm.EditedBitmap = bmp;
         preview.ReplaceEditedBitmap(id, bmp);
         await Task.CompletedTask;
      };

      wizVm.ReadBitmapFromEditorHandler = () => Task.FromResult(hostVm.EditedBitmap);

      var win = new FontImportWizardWindow();
      win.Initialize(wizVm);
      win.Show(this);
      await Task.CompletedTask;
   }

   private async Task OnGlyphSavedAsync()
   {
      _currentCache?.Clear();

      if(DataContext is MainViewModel vm)
         await vm.RenderAsync().ConfigureAwait(true);
   }

   private async Task OpenWorkspaceAsync()
   {
      if(_workspace == null) return;

      var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
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

   private async Task ReinitializeForWorkspaceAsync(string workspaceRoot)
   {
      if(_workspace == null) return;
      if(_applyRuntime == null) return;

      var symbolsRoot = _workspace.ResolveSymbolsFolder(workspaceRoot);

      IGlyphNaming naming = new DefaultGlyphNaming();
      IGlyphRepository repo = new FileSystemGlyphRepository(new FileSystemOptions(symbolsRoot), naming);
      IFontPackProvider packs = new FileSystemFontPackProvider(new FileSystemOptions(symbolsRoot));

      var vm = new MainViewModel(packs, repo)
      {
         Text = (DataContext as MainViewModel)?.Text ?? string.Empty,
         SelectedPackId = (DataContext as MainViewModel)?.SelectedPackId ?? "8x16",
         SelectedStyle = (DataContext as MainViewModel)?.SelectedStyle ?? new Storage.Abstractions.Models.FontStyleId("_base_")
      };

      vm.OpenWorkspaceHandler = OpenWorkspaceAsync;
      vm.CreateSymbolsHandler = CreateSymbolsAsync;
      vm.ImportFontHandler = ImportFontAsync;
      vm.GlyphSavedHandler = OnGlyphSavedAsync;
      vm.GlyphBitmapReplacedHandler = OnGlyphBitmapReplacedAsync;

      await vm.ReloadAsync().ConfigureAwait(true);

      var cache = new TerminalGlyphCache(repo);
      DataContext = vm;
      _applyRuntime(vm, cache);
   }
}
