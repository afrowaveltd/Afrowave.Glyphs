using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Tools;

namespace Editor.Avalonia
{
   public partial class GlyphBrowserWindow : Window
   {
      public GlyphBrowserWindow()
      {
         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }

      public void Initialize(GlyphBrowserViewModel viewModel)
      {
         DataContext = viewModel;

         // Load glyphs on show
         Opened += async (s, e) =>
         {
            await viewModel.LoadGlyphsAsync();
         };
      }
   }
}
