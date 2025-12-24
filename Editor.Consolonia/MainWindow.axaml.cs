using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Core.Naming;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using Storage.FileSystem;
using Tools;
using Tools.FontImport;
using System.Threading.Tasks;

namespace Editor.Consolonia;

public partial class MainWindow : Window
{
   public MainWindow()
   {
      InitializeComponent();

      var vm = new ConsoloniaMainViewModel();
      vm.StartImportHandler = StartImportAsync;
      DataContext = vm;
   }

   private async Task StartImportAsync()
   {
      // Minimal setup similar to Avalonia editor init
      var store = new JsonAppSettingsStore();
      var settingsService = new SettingsService(store);
      await settingsService.InitializeAsync().ConfigureAwait(true);

      var settings = settingsService.Current;
      string workspaceRoot = settings.GetActiveSymbolsRoot();
      string symbolsRoot = SymbolsFolderResolver.ResolveSymbolsRoot(workspaceRoot, createIfMissing: true);

      IGlyphNaming naming = new DefaultGlyphNaming();
      IGlyphRepository repo = new FileSystemGlyphRepository(new FileSystemOptions(symbolsRoot), naming);

      var rasterizer = new FontGlyphRasterizer();
      var importService = new FontImportService(repo, rasterizer);

      var wizVm = new FontImportWizardViewModel(importService)
      {
         PackId = "8x16",
         Style = new FontStyleId("_base_"),
         GlyphSize = new Core.Models.GridSize(8, 16)
      };

      wizVm.PickFontFileHandler = async title =>
      {
         var picker = new FilePickerWindow();
         var startDir = FilePickerWindow.GetDefaultFontsDirectory();
         return await picker.PickAsync(this, title, startDir, ".ttf;.otf").ConfigureAwait(true);
      };

      var win = new FontImportWizardWindow();
      win.Initialize(wizVm);

      GlyphEditorViewModel? editorVm = null;
      wizVm.ApplyBitmapToEditorHandler = async bmp =>
      {
         if(bmp == null)
            return;

         // Clone so cancel doesn't mutate the rasterized original.
         editorVm = new GlyphEditorViewModel(bmp.Clone());
         var editor = new GlyphEditorWindow();
         var accepted = await editor.EditAsync(editorVm, (Window)this).ConfigureAwait(true);

         if(editor.SkipRequested)
         {
            editorVm = null;
            await wizVm.SkipAsync().ConfigureAwait(true);
            return;
         }

         if(!accepted)
         {
            editorVm = null;
            return;
         }

         // If accepted via 'N' (save-next), we can auto-advance by triggering NextAsync.
         // Key.N and Enter both set accepted; we treat both the same here (user still can use Next button).
      };

      wizVm.ReadBitmapFromEditorHandler = () => Task.FromResult(editorVm?.Bitmap ?? wizVm.CurrentBitmap);

      await win.ShowDialog(this).ConfigureAwait(true);
   }

   private void OnExit(object sender, RoutedEventArgs e)
   {
      var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
      lifetime!.Shutdown();
   }
}