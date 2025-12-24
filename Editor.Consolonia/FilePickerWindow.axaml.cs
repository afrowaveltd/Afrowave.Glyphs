using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Iciclecreek.Avalonia.WindowManager;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Editor.Consolonia;

public partial class FilePickerWindow : ManagedWindow
{
   private TaskCompletionSource<string?>? _tcs;

   public FilePickerWindow()
   {
      InitializeComponent();
   }

   private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

   public Task<string?> PickAsync(Window owner, string title, string startDirectory, string filterText)
   {
      Title = title;
      var vm = new FilePickerViewModel(startDirectory)
      {
         FilterText = filterText
      };

      DataContext = vm;
      _tcs = new TaskCompletionSource<string?>();

      Closed += (_, _) => _tcs.TrySetResult(null);

      Show(owner);
      return _tcs.Task;
   }

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

      var selected = vm.GetSelectedFullPath();
      if(string.IsNullOrWhiteSpace(selected) || !File.Exists(selected))
         return;

      _tcs?.TrySetResult(selected);
      Close();
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
            return Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

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
