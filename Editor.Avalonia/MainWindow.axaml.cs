using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Editor.Avalonia.Services;
using Tools;

namespace Editor.Avalonia;

public partial class MainWindow : Window
{
   public MainWindow() => InitializeComponent();

   private void InitializeComponent()
   {
      AvaloniaXamlLoader.Load(this);
   }

   public void SetTerminalCache(TerminalGlyphCache cache)
   {
      var terminal = this.FindControl<TerminalPreviewControl>("Terminal")!;
      terminal.SetCache(cache);

      // 2) Debug: cell clicked -> prove MVVM/binding works even without glyph files
      terminal.CellClicked += (x, y) =>
      {
         if(DataContext is MainViewModel vm)
            vm.Text = $"Clicked cell: {x},{y}";
      };
   }
}
