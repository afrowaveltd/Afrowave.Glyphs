using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Editor.Consolonia;

public sealed class ConsoloniaMainViewModel : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;

   public Func<Task>? StartImportHandler { get; set; }

   public Tools.AsyncCommand StartImportCommand { get; }

   public ConsoloniaMainViewModel()
   {
      StartImportCommand = new Tools.AsyncCommand(() => StartImportHandler?.Invoke() ?? Task.CompletedTask);
   }

   private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
   {
      if(Equals(field, value)) return false;
      field = value;
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      return true;
   }
}
