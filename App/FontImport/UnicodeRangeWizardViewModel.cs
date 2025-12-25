using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.FontImport
{
   public sealed class UnicodeRangeWizardViewModel : INotifyPropertyChanged
   {
      private readonly IGlyphRepository _repo;

      public event PropertyChangedEventHandler? PropertyChanged;

      private int _startCodePoint = 32;
      public int StartCodePoint
      {
         get => _startCodePoint;
         set => Set(ref _startCodePoint, value);
      }

      private int _endCodePoint = 126;
      public int EndCodePoint
      {
         get => _endCodePoint;
         set => Set(ref _endCodePoint, value);
      }

      private int _currentCodePoint;
      public int CurrentCodePoint
      {
         get => _currentCodePoint;
         private set
         {
            if(Set(ref _currentCodePoint, value))
            {
               Notify(nameof(CurrentChar));
               Notify(nameof(CurrentCharDisplay));
               Notify(nameof(ProgressText));
               Notify(nameof(HasPrevious));
               Notify(nameof(HasNext));
            }
         }
      }

      private string _packId = "8x16";
      public string PackId
      {
         get => _packId;
         set => Set(ref _packId, value);
      }

      private FontStyleId _style = new FontStyleId("_base_");
      public FontStyleId Style
      {
         get => _style;
         set => Set(ref _style, value);
      }

      private GridSize _glyphSize = new GridSize(8, 16);
      public GridSize GlyphSize
      {
         get => _glyphSize;
         set => Set(ref _glyphSize, value);
      }

      private GlyphBitmap? _currentBitmap;
      public GlyphBitmap? CurrentBitmap
      {
         get => _currentBitmap;
         set => Set(ref _currentBitmap, value);
      }

      public char CurrentChar => (char)CurrentCodePoint;
      public string CurrentCharDisplay => $"U+{CurrentCodePoint:X4} '{CurrentChar}'";
      public string ProgressText => $"{CurrentCodePoint - StartCodePoint + 1} / {EndCodePoint - StartCodePoint + 1}";
      
      public bool HasPrevious => CurrentCodePoint > StartCodePoint;
      public bool HasNext => CurrentCodePoint < EndCodePoint;

      public AsyncCommand PreviousCommand { get; }
      public AsyncCommand NextCommand { get; }
      public AsyncCommand SaveCommand { get; }
      public AsyncCommand SkipCommand { get; }

      public Func<GlyphBitmap, Task<bool>>? EditBitmapHandler { get; set; }

      public UnicodeRangeWizardViewModel(IGlyphRepository repo)
      {
         _repo = repo ?? throw new ArgumentNullException(nameof(repo));

         PreviousCommand = new AsyncCommand(PreviousAsync);
         NextCommand = new AsyncCommand(NextAsync);
         SaveCommand = new AsyncCommand(SaveCurrentAsync);
         SkipCommand = new AsyncCommand(SkipAsync);
      }

      public void Start()
      {
         CurrentCodePoint = StartCodePoint;
         _ = LoadCurrentGlyphAsync();
      }

      private async Task LoadCurrentGlyphAsync()
      {
         var id = GlyphId.FromUnicode(CurrentCodePoint);
         
         if(await _repo.ExistsAsync(PackId, Style, id, CancellationToken.None).ConfigureAwait(false))
         {
            var glyph = await _repo.LoadAsync(PackId, Style, id, CancellationToken.None).ConfigureAwait(false);
            CurrentBitmap = glyph.Bitmap.Clone();
         }
         else
         {
            CurrentBitmap = new GlyphBitmap(GlyphSize, new byte[GlyphBitmap.GetByteLength(GlyphSize)]);
         }
      }

      private async Task PreviousAsync()
      {
         if(!HasPrevious) return;
         CurrentCodePoint--;
         await LoadCurrentGlyphAsync().ConfigureAwait(false);
      }

      private async Task NextAsync()
      {
         if(!HasNext) return;
         CurrentCodePoint++;
         await LoadCurrentGlyphAsync().ConfigureAwait(false);
      }

      private async Task SaveCurrentAsync()
      {
         if(CurrentBitmap == null) return;

         var id = GlyphId.FromUnicode(CurrentCodePoint);
         var glyph = new Glyph(id, GlyphSize, CurrentBitmap.Clone());
         await _repo.SaveAsync(PackId, Style, glyph, CancellationToken.None).ConfigureAwait(false);

         if(HasNext)
            await NextAsync().ConfigureAwait(false);
      }

      private async Task SkipAsync()
      {
         if(HasNext)
            await NextAsync().ConfigureAwait(false);
      }

      private void Notify(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
      {
         if(Equals(field, value)) return false;
         field = value;
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
         return true;
      }
   }
}
