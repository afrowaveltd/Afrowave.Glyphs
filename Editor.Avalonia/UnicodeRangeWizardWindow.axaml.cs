using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Core.Models;
using Editor.Avalonia.Controls;
using Storage.Abstractions.Models;
using System.Threading.Tasks;
using Tools;
using Tools.FontImport;

namespace Editor.Avalonia
{
   public partial class UnicodeRangeWizardWindow : Window
   {
      private UnicodeRangeWizardViewModel? _vm;
      private GlyphPreviewControl? _preview;

      public UnicodeRangeWizardWindow()
      {
         InitializeComponent();
      }

      private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

      public void Initialize(UnicodeRangeWizardViewModel vm)
      {
         _vm = vm;
         DataContext = vm;

         var border = this.FindControl<Border>("GlyphPreviewBorder");
         if(border != null)
         {
            _preview = new GlyphPreviewControl
            {
               PackId = vm.PackId,
               Style = vm.Style,
               GlyphSize = vm.GlyphSize,
               PixelScale = 4
            };

            border.Child = _preview;

            _preview.GlyphEdited += (id, bmp) =>
            {
               if(_vm != null)
                  _vm.CurrentBitmap = bmp;
            };
         }

         // Subscribe to property changes to update preview
         vm.PropertyChanged += (s, e) =>
         {
            if (e.PropertyName == nameof(UnicodeRangeWizardViewModel.CurrentCodePoint) ||
                e.PropertyName == nameof(UnicodeRangeWizardViewModel.CurrentBitmap))
            {
               Dispatcher.UIThread.Post(UpdatePreview);
            }
         };
      }

      private void OnStart(object? sender, RoutedEventArgs e)
      {
         if(_vm == null) return;
         _vm.Start();
         UpdatePreview();
      }

      private async void OnEdit(object? sender, RoutedEventArgs e)
      {
         if(_vm?.CurrentBitmap == null) return;

         var editorVm = new GlyphEditorViewModel(_vm.CurrentBitmap.Clone());
         var editor = new GlyphEditorWindow();
         var accepted = await editor.EditAsync(editorVm, this).ConfigureAwait(true);

         if(accepted)
         {
            _vm.CurrentBitmap = editorVm.Bitmap;
            UpdatePreview();
         }
      }

      private void OnClear(object? sender, RoutedEventArgs e)
      {
         if(_vm == null) return;
         _vm.CurrentBitmap = new GlyphBitmap(_vm.GlyphSize, new byte[GlyphBitmap.GetByteLength(_vm.GlyphSize)]);
         UpdatePreview();
      }

      private void OnClose(object? sender, RoutedEventArgs e)
      {
         Close();
      }

      private void UpdatePreview()
      {
         if(_preview == null || _vm?.CurrentBitmap == null) return;
         
         var id = GlyphId.FromUnicode(_vm.CurrentCodePoint);
         _preview.GlyphId = id;
         _preview.ReplaceEditedBitmap(id, _vm.CurrentBitmap);
      }
   }
}
