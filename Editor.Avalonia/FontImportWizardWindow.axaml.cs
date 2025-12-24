using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Tools.FontImport;
using System;
using System.Threading.Tasks;

namespace Editor.Avalonia;

public partial class FontImportWizardWindow : Window
{
   public FontImportWizardWindow()
   {
      InitializeComponent();
   }

   private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

   public void Initialize(FontImportWizardViewModel vm)
   {
      DataContext = vm;

      vm.PickFontFileHandler = PickFontFileAsync;
      vm.CloseHandler = CloseAsync;
   }

   private async Task<string?> PickFontFileAsync(string title)
   {
      var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
      {
         Title = title,
         AllowMultiple = false,
         FileTypeFilter = new[]
         {
            new FilePickerFileType("Fonts")
            {
               Patterns = new[] { "*.ttf", "*.otf", "*.ttc" }
            }
         }
      }).ConfigureAwait(true);

      if(files == null || files.Count == 0)
         return null;

      return files[0].Path.LocalPath;
   }

   private Task CloseAsync()
   {
      Close();
      return Task.CompletedTask;
   }
}
