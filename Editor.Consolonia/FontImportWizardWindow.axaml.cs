using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Tools.FontImport;
using System.Threading.Tasks;

namespace Editor.Consolonia;

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
      vm.CloseHandler = CloseAsync;
   }

   private Task CloseAsync()
   {
      Close();
      return Task.CompletedTask;
   }
}
