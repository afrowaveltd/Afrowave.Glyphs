using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Editor.Consolonia;

public sealed class FilePickerViewModel : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;

   private string _currentDirectory;
   public string CurrentDirectory { get => _currentDirectory; set { if(Set(ref _currentDirectory, value)) Refresh(); } }

   private string? _selectedItem;
   public string? SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }

   private string? _filterText;
   public string? FilterText { get => _filterText; set { if(Set(ref _filterText, value)) Refresh(); } }

   public ObservableCollection<string> Items { get; } = new();

   public FilePickerViewModel(string startDirectory)
   {
      _currentDirectory = startDirectory;
      Refresh();
   }

   public void Refresh()
   {
      Items.Clear();

      if(string.IsNullOrWhiteSpace(CurrentDirectory) || !Directory.Exists(CurrentDirectory))
         return;

      try
      {
         Items.Add("..");

         foreach(var d in Directory.EnumerateDirectories(CurrentDirectory).OrderBy(Path.GetFileName))
            Items.Add(Path.GetFileName(d) ?? d);

         var files = Directory.EnumerateFiles(CurrentDirectory)
            .Where(f => MatchesFilter(f, FilterText))
            .OrderBy(Path.GetFileName);

         foreach(var f in files)
            Items.Add(Path.GetFileName(f) ?? f);
      }
      catch
      {
         // ignore directory enumeration errors
      }
   }

   public bool TryNavigateInto(string? item)
   {
      if(string.IsNullOrWhiteSpace(item))
         return false;

      if(item == "..")
      {
         var parent = Directory.GetParent(CurrentDirectory);
         if(parent == null) return false;
         CurrentDirectory = parent.FullName;
         SelectedItem = null;
         return true;
      }

      var candidate = Path.Combine(CurrentDirectory, item);
      if(Directory.Exists(candidate))
      {
         CurrentDirectory = candidate;
         SelectedItem = null;
         return true;
      }

      return false;
   }

   public string? GetSelectedFullPath()
   {
      if(string.IsNullOrWhiteSpace(SelectedItem) || SelectedItem == "..")
         return null;

      return Path.Combine(CurrentDirectory, SelectedItem);
   }

   private static bool MatchesFilter(string path, string? filter)
   {
      if(string.IsNullOrWhiteSpace(filter))
         return true;

      var ext = Path.GetExtension(path);
      return filter
         .Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
         .Any(f => ext.Equals(f.StartsWith('.') ? f : "." + f, StringComparison.OrdinalIgnoreCase));
   }

   private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
   {
      if(Equals(field, value)) return false;
      field = value;
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      return true;
   }
}
