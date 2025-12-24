using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.FontImport
{
   public sealed class FontImportWizardViewModel : INotifyPropertyChanged
   {
      private readonly FontImportService _import;

      public event PropertyChangedEventHandler? PropertyChanged;

      public ObservableCollection<GridSize> Presets { get; } = new ObservableCollection<GridSize>
      {
         new GridSize(5, 8),
         new GridSize(5, 10),
         new GridSize(8, 16),
         new GridSize(16, 16),
         new GridSize(32, 32)
      };

      private string? _fontPath;
      public string? FontPath { get => _fontPath; set => Set(ref _fontPath, value); }

      private string _packId = "8x16";
      public string PackId { get => _packId; set => Set(ref _packId, value); }

      private FontStyleId _style = new FontStyleId("_base_");
      public FontStyleId Style { get => _style; set => Set(ref _style, value); }

      private GridSize _glyphSize = new GridSize(8, 16);
      public GridSize GlyphSize { get => _glyphSize; set => Set(ref _glyphSize, value); }

      private int _rangeStart = 0x20;
      public int RangeStart { get => _rangeStart; set => Set(ref _rangeStart, value); }

      private int _rangeEnd = 0x7E;
      public int RangeEnd { get => _rangeEnd; set => Set(ref _rangeEnd, value); }

      private int _currentCodePoint;
      public int CurrentCodePoint { get => _currentCodePoint; private set { if(Set(ref _currentCodePoint, value)) Notify(nameof(CurrentChar)); Notify(nameof(CurrentGlyphIdText)); } }

      public string CurrentChar => _currentCodePoint == 0 ? string.Empty : char.ConvertFromUtf32(_currentCodePoint);
      public string CurrentGlyphIdText => _currentCodePoint == 0 ? string.Empty : GlyphId.FromUnicode(_currentCodePoint).ToString();

      private GlyphBitmap? _currentBitmap;
      public GlyphBitmap? CurrentBitmap { get => _currentBitmap; private set => Set(ref _currentBitmap, value); }

      private int _currentIndex;
      public int CurrentIndex { get => _currentIndex; private set { if(Set(ref _currentIndex, value)) Notify(nameof(ProgressText)); } }

      private int _totalCount;
      public int TotalCount { get => _totalCount; private set { if(Set(ref _totalCount, value)) Notify(nameof(ProgressText)); } }

      public string ProgressText => TotalCount <= 0 ? string.Empty : $"{CurrentIndex}/{TotalCount}";

      private string? _status;
      public string? Status { get => _status; private set => Set(ref _status, value); }

      private bool _isRunning;
      public bool IsRunning { get => _isRunning; private set => Set(ref _isRunning, value); }

      public AsyncCommand StartCommand { get; }
      public AsyncCommand PickFontCommand { get; }
      public AsyncCommand NextCommand { get; }
      public AsyncCommand SkipCommand { get; }
      public AsyncCommand PreviousCommand { get; }
      public AsyncCommand CancelCommand { get; }
      public AsyncCommand FinishCommand { get; }

      private CancellationTokenSource? _cts;

      private readonly List<int> _codePoints = new List<int>();
      private int _position;

      // Host-provided hooks
      public Func<string, Task<string?>>? PickFontFileHandler { get; set; }
      public Func<Task>? CloseHandler { get; set; }
      public Func<GlyphBitmap?, Task>? ApplyBitmapToEditorHandler { get; set; }
      public Func<Task<GlyphBitmap?>>? ReadBitmapFromEditorHandler { get; set; }

      public FontImportWizardViewModel(FontImportService import)
      {
         _import = import ?? throw new ArgumentNullException(nameof(import));

         PickFontCommand = new AsyncCommand(PickFontAsync);
         StartCommand = new AsyncCommand(StartAsync);
         NextCommand = new AsyncCommand(NextAsync);
         SkipCommand = new AsyncCommand(SkipAsync);
         PreviousCommand = new AsyncCommand(PreviousAsync);
         CancelCommand = new AsyncCommand(CancelAsync);
         FinishCommand = new AsyncCommand(FinishAsync);
      }

      public async Task PickFontAsync()
      {
         if(PickFontFileHandler == null) return;
         var picked = await PickFontFileHandler("Select font (.ttf/.otf)").ConfigureAwait(false);
         if(!string.IsNullOrWhiteSpace(picked))
            FontPath = picked;
      }

      public async Task StartAsync()
      {
         if(IsRunning) return;
         if(string.IsNullOrWhiteSpace(FontPath))
         {
            Status = "No font selected.";
            return;
         }

         if(RangeEnd < RangeStart)
         {
            Status = "Range end must be >= range start.";
            return;
         }

         IsRunning = true;
         _cts = new CancellationTokenSource();
         _codePoints.Clear();
         for(int cp = RangeStart; cp <= RangeEnd; cp++)
            _codePoints.Add(cp);

         _position = 0;
         TotalCount = _codePoints.Count;
         CurrentIndex = TotalCount > 0 ? 1 : 0;
         Status = "Started.";

         await LoadCurrentAsync().ConfigureAwait(false);
      }

      private async Task LoadCurrentAsync()
      {
         if(_cts == null) return;

         if(_position < 0) _position = 0;
         if(_position >= _codePoints.Count)
         {
            Status = "Done.";
            CurrentCodePoint = 0;
            CurrentBitmap = null;
            if(ApplyBitmapToEditorHandler != null)
               await ApplyBitmapToEditorHandler(null).ConfigureAwait(false);
            return;
         }

         CurrentCodePoint = _codePoints[_position];
         CurrentIndex = _codePoints.Count == 0 ? 0 : (_position + 1);

         try
         {
            var bmp = await new FontGlyphRasterizer().RasterizeAsync(FontPath!, CurrentCodePoint, GlyphSize, _cts.Token).ConfigureAwait(false);
            CurrentBitmap = bmp;
            if(ApplyBitmapToEditorHandler != null)
               await ApplyBitmapToEditorHandler(bmp).ConfigureAwait(false);

            Status = "Loaded.";
         }
         catch(Exception ex)
         {
            Status = ex.Message;
            CurrentBitmap = null;
         }
      }

      public async Task NextAsync()
      {
         if(!IsRunning || _cts == null) return;
         if(CurrentCodePoint == 0) return;

         try
         {
            GlyphBitmap? edited = CurrentBitmap; // Use the current bitmap if no editor handler is present
            if(ReadBitmapFromEditorHandler != null)
               edited = await ReadBitmapFromEditorHandler().ConfigureAwait(false);

            if(edited != null)
            {
               await _import.SaveGlyphAsync(PackId, Style, GlyphId.FromUnicode(CurrentCodePoint), GlyphSize, edited, _cts.Token).ConfigureAwait(false);
               Status = "Saved.";
            }
            else
            {
                // Save the current bitmap if no editing is done
                await _import.SaveGlyphAsync(PackId, Style, GlyphId.FromUnicode(CurrentCodePoint), GlyphSize, CurrentBitmap, _cts.Token).ConfigureAwait(false);
                Status = "Saved without editing.";
            }
         }
         catch(Exception ex)
         {
            Status = ex.Message;
            return;
         }

         _position++;
         await LoadCurrentAsync().ConfigureAwait(false);
      }

      public async Task SkipAsync()
      {
         if(!IsRunning) return;
         Status = "Skipped.";
         _position++;
         await LoadCurrentAsync().ConfigureAwait(false);
      }

      public async Task PreviousAsync()
      {
         if(!IsRunning) return;
         if(_codePoints.Count == 0) return;

         _position--;
         if(_position < 0) _position = 0;
         Status = "Previous.";
         await LoadCurrentAsync().ConfigureAwait(false);
      }

      public Task CancelAsync()
      {
         _cts?.Cancel();
         IsRunning = false;
         Status = "Cancelled.";
         return Task.CompletedTask;
      }

      public async Task FinishAsync()
      {
         await CancelAsync().ConfigureAwait(false);
         if(CloseHandler != null)
            await CloseHandler().ConfigureAwait(false);
      }

      private void Notify(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

      private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
      {
         if(Equals(field, value)) return false;
         field = value;
         Notify(name!);
         return true;
      }
   }
}
