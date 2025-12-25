using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;

namespace Tools
{
   public sealed class GlyphBrowserViewModel : INotifyPropertyChanged
   {
      private readonly IGlyphRepository _repo;
      private readonly string _packId;
      private readonly FontStyleId _style;
      private readonly GridSize _glyphSize;

      public event PropertyChangedEventHandler? PropertyChanged;

      public ObservableCollection<GlyphBrowserItem> Glyphs { get; } = new ObservableCollection<GlyphBrowserItem>();

      private GlyphBrowserItem? _selectedGlyph;
      public GlyphBrowserItem? SelectedGlyph
      {
         get => _selectedGlyph;
         set
         {
            if(Set(ref _selectedGlyph, value))
            {
               _ = LoadSelectedGlyphAsync();
            }
         }
      }

      private GlyphBitmap? _previewBitmap;
      public GlyphBitmap? PreviewBitmap
      {
         get => _previewBitmap;
         private set => Set(ref _previewBitmap, value);
      }

      private string _searchText = string.Empty;
      public string SearchText
      {
         get => _searchText;
         set
         {
            if(Set(ref _searchText, value))
               FilterGlyphs();
         }
      }

      private string _statusText = "Loading...";
      public string StatusText
      {
         get => _statusText;
         private set => Set(ref _statusText, value);
      }

      public AsyncCommand LoadCommand { get; }
      public AsyncCommand PreviousCommand { get; }
      public AsyncCommand NextCommand { get; }
      public AsyncCommand EditCommand { get; }
      public AsyncCommand RefreshCommand { get; }

      public Func<GlyphId, GlyphBitmap, Task>? EditGlyphHandler { get; set; }
      public Action? CloseWindowHandler { get; set; } // NEW: Handler to close window after Edit

      public GlyphBrowserViewModel(IGlyphRepository repo, string packId, FontStyleId style, GridSize glyphSize)
      {
         _repo = repo ?? throw new ArgumentNullException(nameof(repo));
         _packId = packId;
         _style = style;
         _glyphSize = glyphSize;

            LoadCommand = new AsyncCommand(LoadGlyphsAsync);
            PreviousCommand = new AsyncCommand(SelectPreviousAsync);
            NextCommand = new AsyncCommand(SelectNextAsync);
            EditCommand = new AsyncCommand(EditSelectedGlyphAsync);
            RefreshCommand = new AsyncCommand(RefreshCurrentGlyphAsync);
         }

      public async Task LoadGlyphsAsync()
      {
         StatusText = "Loading glyphs...";
         Glyphs.Clear();

         try
         {
            var glyphInfos = await _repo.ListGlyphsAsync(_packId, _style, CancellationToken.None);

            foreach(var info in glyphInfos.OrderBy(g => g.Id.IsUnicode ? g.Id.UnicodeCodePoint : 0x110000))
            {
               var id = info.Id;
               var exists = await _repo.ExistsAsync(_packId, _style, id, CancellationToken.None);
               if(exists)
               {
                  var displayName = GetDisplayName(id);
                  Glyphs.Add(new GlyphBrowserItem(id, displayName));
               }
            }

            StatusText = $"Loaded {Glyphs.Count} glyphs from {_packId}/{_style.Name}";

            if(Glyphs.Count > 0)
               SelectedGlyph = Glyphs[0];
         }
         catch(Exception ex)
         {
            StatusText = $"Error loading glyphs: {ex.Message}";
         }
      }

      private async Task LoadSelectedGlyphAsync()
      {
         if(SelectedGlyph == null)
         {
            PreviewBitmap = null;
            return;
         }

         try
         {
            var glyph = await _repo.LoadAsync(_packId, _style, SelectedGlyph.GlyphId, CancellationToken.None);
            PreviewBitmap = glyph.Bitmap;
            StatusText = $"Loaded: {SelectedGlyph.DisplayName} (Size: {glyph.Size})";
         }
         catch(Exception ex)
         {
            StatusText = $"Error loading glyph: {ex.Message}";
            PreviewBitmap = null;
         }
      }

      private Task SelectPreviousAsync()
      {
         if(SelectedGlyph == null) return Task.CompletedTask;
         
         var index = Glyphs.IndexOf(SelectedGlyph);
         if(index > 0)
            SelectedGlyph = Glyphs[index - 1];

         return Task.CompletedTask;
      }

      private Task SelectNextAsync()
      {
         if(SelectedGlyph == null) return Task.CompletedTask;
         
         var index = Glyphs.IndexOf(SelectedGlyph);
         if(index < Glyphs.Count - 1)
            SelectedGlyph = Glyphs[index + 1];

         return Task.CompletedTask;
      }

      private async Task EditSelectedGlyphAsync()
      {
         if(SelectedGlyph == null || PreviewBitmap == null || EditGlyphHandler == null)
            return;

         await EditGlyphHandler(SelectedGlyph.GlyphId, PreviewBitmap);

         // Activate main window instead of closing browser
         CloseWindowHandler?.Invoke();
      }

      private async Task RefreshCurrentGlyphAsync()
      {
         // Reload the currently selected glyph from repository
         if(SelectedGlyph == null) return;

         await LoadSelectedGlyphAsync();
         StatusText = $"Refreshed: {SelectedGlyph.DisplayName}";
      }

      private void FilterGlyphs()
      {
         // TODO: Implement search/filter
         // For now, just update status
         if(string.IsNullOrWhiteSpace(SearchText))
         {
            StatusText = $"Showing all {Glyphs.Count} glyphs";
         }
         else
         {
            StatusText = $"Search: {SearchText}";
         }
      }

      private static string GetDisplayName(GlyphId id)
      {
         if(id.IsUnicode)
         {
            var code = id.UnicodeCodePoint!.Value;
            var charStr = code >= 32 && code <= 126 ? $" '{(char)code}'" : "";
            return $"U+{code:X4}{charStr}";
         }
         else
         {
            return $"#{id.InternalName}";
         }
      }

      private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
      {
         if(Equals(field, value)) return false;
         field = value;
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
         return true;
      }
   }

   public sealed class GlyphBrowserItem
   {
      public GlyphId GlyphId { get; }
      public string DisplayName { get; }

      public GlyphBrowserItem(GlyphId glyphId, string displayName)
      {
         GlyphId = glyphId;
         DisplayName = displayName;
      }
   }
}
