using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Iciclecreek.Avalonia.WindowManager;
using System.Threading.Tasks;

namespace Editor.Consolonia;

public partial class GlyphEditorWindow : ManagedWindow
{
   private bool _accepted;
   private bool _skip;

   public GlyphEditorWindow()
   {
      InitializeComponent();
      KeyDown += OnKeyDown;
   }

   private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

   public async Task<bool> EditAsync(GlyphEditorViewModel vm, Window owner)
   {
      DataContext = vm;
      _accepted = false;
      _skip = false;
      Focus();
      await ShowDialog(owner).ConfigureAwait(true);
      return _accepted;
   }

   public bool SkipRequested => _skip;

   private void OnKeyDown(object? sender, KeyEventArgs e)
   {
      if(DataContext is not GlyphEditorViewModel vm)
         return;

      switch(e.Key)
      {
         case Key.Left:
         case Key.A:
            vm.Move(-1, 0);
            e.Handled = true;
            break;
         case Key.Right:
         case Key.D:
            vm.Move(1, 0);
            e.Handled = true;
            break;
         case Key.Up:
         case Key.W:
            vm.Move(0, -1);
            e.Handled = true;
            break;
         case Key.Down:
         case Key.S:
            vm.Move(0, 1);
            e.Handled = true;
            break;
         case Key.Space:
            vm.Toggle();
            e.Handled = true;
            break;
         case Key.C:
            vm.Clear();
            e.Handled = true;
            break;
         case Key.I:
            vm.Invert();
            e.Handled = true;
            break;
         case Key.F:
            vm.Fill();
            e.Handled = true;
            break;
         case Key.Enter:
            _accepted = true;
            Close();
            e.Handled = true;
            break;
         case Key.N:
            _accepted = true;
            Close();
            e.Handled = true;
            break;
         case Key.K:
            _accepted = false;
            _skip = true;
            Close();
            e.Handled = true;
            break;
         case Key.Escape:
            _accepted = false;
            Close();
            e.Handled = true;
            break;
      }
   }
}
