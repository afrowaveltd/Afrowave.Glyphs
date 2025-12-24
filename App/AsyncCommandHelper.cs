using System;
using System.Reflection;
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
            RaiseCanExecuteChanged();
            await _execute().ConfigureAwait(true);
         }
         finally
         {
            _isRunning = false;
            RaiseCanExecuteChanged();
         }
      }

      private void RaiseCanExecuteChanged()
      {
         var handler = CanExecuteChanged;
         if(handler is null) return;

         // Marshal to Avalonia UI thread when available. Keep this library usable without a hard Avalonia reference.
         if(TryPostToAvaloniaUiThread(() => handler(this, EventArgs.Empty))) return;

         handler(this, EventArgs.Empty);
      }

      private static bool TryPostToAvaloniaUiThread(Action action)
      {
         try
         {
            // Avalonia.Threading.Dispatcher.UIThread
            var dispatcherType = Type.GetType("Avalonia.Threading.Dispatcher, Avalonia.Base", throwOnError: false);
            if(dispatcherType is null) return false;

            var uiThreadProperty = dispatcherType.GetProperty("UIThread", BindingFlags.Public | BindingFlags.Static);
            var uiThread = uiThreadProperty?.GetValue(null);
            if(uiThread is null) return false;

            // bool CheckAccess()
            var checkAccessMethod = uiThread.GetType().GetMethod("CheckAccess", BindingFlags.Public | BindingFlags.Instance);
            var hasAccess = checkAccessMethod is null ? (bool?)null : (bool?)checkAccessMethod.Invoke(uiThread, Array.Empty<object>());
            if(hasAccess == true)
            {
               action();
               return true;
            }

            // void Post(Action)
            var postMethod = uiThread.GetType().GetMethod("Post", BindingFlags.Public | BindingFlags.Instance, binder: null, types: new[] { typeof(Action) }, modifiers: null);
            if(postMethod is null) return false;

            postMethod.Invoke(uiThread, new object[] { action });
            return true;
         }
         catch
         {
            return false;
         }
      }
   }
}
