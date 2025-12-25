using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tools;

namespace Editor.Consolonia;

public partial class FilePickerWindow : Window
{
   private TaskCompletionSource<string?>? _tcs;

   public FilePickerWindow()
   {
      InitializeComponent();
   }

   private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

   public Task<string?> PickAsync(Window owner, string title, string startDirectory, string filterText, bool allowFolderSelection = false)
   {
      Title = title;
      var vm = new FilePickerViewModel(startDirectory)
      {
         FilterText = filterText
      };

      DataContext = vm;
      _tcs = new TaskCompletionSource<string?>();
      _allowFolderSelection = allowFolderSelection;

      Closed += (_, _) => _tcs.TrySetResult(null);

      // Consolonia: Show as non-modal window (can't use ShowDialog from another window)
      Show();

      return _tcs.Task;
   }

   private bool _allowFolderSelection;

   private void OnOpen(object? sender, RoutedEventArgs e)
   {
      if(DataContext is not FilePickerViewModel vm)
         return;

      if(vm.TryNavigateInto(vm.SelectedItem))
         return;

      var selected = vm.GetSelectedFullPath();
      if(selected != null && Directory.Exists(selected))
      {
         vm.CurrentDirectory = selected;
         return;
      }
   }

   private void OnSelect(object? sender, RoutedEventArgs e)
   {
      if(DataContext is not FilePickerViewModel vm)
         return;

      // If folder selection is allowed and no item is selected (or ".." is selected),
      // we might want to return the current directory.
      // However, usually users select a folder in the list.
      
      var selected = vm.GetSelectedFullPath();
      
      // Fallback: if nothing selected, and we are in folder mode, return current directory
      if (string.IsNullOrWhiteSpace(selected) && _allowFolderSelection)
      {
          _tcs?.TrySetResult(vm.CurrentDirectory);
          Close();
          return;
      }

      if(string.IsNullOrWhiteSpace(selected))
         return;

      if (_allowFolderSelection)
      {
          if (Directory.Exists(selected))
          {
              _tcs?.TrySetResult(selected);
              Close();
              return;
          }
          // If they selected a file but we want a folder, maybe return the parent? 
          // Or just ignore. For now, let's assume they must select a directory.
      }
      else
      {
          if (File.Exists(selected))
          {
              _tcs?.TrySetResult(selected);
              Close();
              return;
          }
      }
   }

   private void OnCancel(object? sender, RoutedEventArgs e)
   {
      _tcs?.TrySetResult(null);
      Close();
   }

   public static string GetDefaultFontsDirectory()
   {
      try
      {
         if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
         {
            // Windows 11+ user fonts
            return AppPaths.GetSystemFontsDirectory();
         }

         if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "/System/Library/Fonts";

         // Linux is distro-dependent; pick a common path.
         if(Directory.Exists("/usr/share/fonts"))
            return "/usr/share/fonts";
      }
      catch
      {
      }

      return Environment.CurrentDirectory;
   }
}
