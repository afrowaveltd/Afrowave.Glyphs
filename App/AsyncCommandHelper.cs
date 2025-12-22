using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Tools
{
   public sealed class AsyncCommand : ICommand
   {
      private readonly Func<Task> _execute;
      private bool _isRunning;

      public event EventHandler? CanExecuteChanged;

      public AsyncCommand(Func<Task> execute)
      {
         _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      }

      public bool CanExecute(object? parameter) => !_isRunning;

      public async void Execute(object? parameter)
      {
         if(_isRunning) return;

         try
         {
            _isRunning = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            await _execute().ConfigureAwait(false);
         }
         finally
         {
            _isRunning = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
         }
      }
   }
}
