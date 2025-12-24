using Core.Models;
using Core.Text;
using Runtime;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tools
{
   public sealed class MainViewModel : INotifyPropertyChanged
   {
      private readonly IFontPackProvider _packs;
      private readonly IGlyphRepository _repo;
      private readonly GlyphResolver _resolver;
      private readonly TextToTerminalRenderer _renderer;

      public IGlyphRepository Repository => _repo;

      public event PropertyChangedEventHandler? PropertyChanged;

      public ObservableCollection<string> PackIds { get; } = new ObservableCollection<string>();
      public ObservableCollection<FontStyleId> Styles { get; } = new ObservableCollection<FontStyleId>();

      public ObservableCollection<GridSize> GlyphSizePresets { get; } = new ObservableCollection<GridSize>
      {
         // Common character displays (HD44780 and similar)
         new GridSize(5, 8),
         new GridSize(5, 10),

         // Editor / terminal-friendly sizes
         new GridSize(8, 16),
         new GridSize(16, 16),
         new GridSize(32, 32)
      };

      private string _selectedPackId = "8x16";
      public string SelectedPackId
      {
         get => _selectedPackId;
         set
         {
            if(Set(ref _selectedPackId, value))
            {
               UpdateSelectedGlyphSizeFromPackId(value);
               _ = ReloadStylesAndRenderAsync();
            }
         }
      }

      private FontStyleId _selectedStyle = new FontStyleId("_base_");
      public FontStyleId SelectedStyle
      {
         get => _selectedStyle;
         set
         {
            if(Set(ref _selectedStyle, value))
               _ = RenderAsync();
         }
      }

      private GlyphId _selectedGlyph = GlyphId.FromInternal("missing");
      public GlyphId SelectedGlyph
      {
         get => _selectedGlyph;
         set
         {
            if(Set(ref _selectedGlyph, value))
            {
               // SelectedGlyphText je odvozená vlastnost, musíme ji oznámit ručně
               Notify(nameof(SelectedGlyphText));
            }
         }
      }

      public string SelectedGlyphText => _selectedGlyph.ToString();

      private string _text = "Hello BOS 👋\r\nAfrowave Glyphs!";
      public string Text
      {
         get => _text;
         set
         {
            if(Set(ref _text, value))
               _ = RenderAsync();
         }
      }

      private TerminalBuffer? _previewBuffer;
      public TerminalBuffer? PreviewBuffer
      {
         get => _previewBuffer;
         private set => Set(ref _previewBuffer, value);
      }

      private GridSize _selectedGlyphSize = new GridSize(8, 16);
      public GridSize SelectedGlyphSize
      {
         get => _selectedGlyphSize;
         set
         {
            if(Set(ref _selectedGlyphSize, value))
            {
               // keep pack id in sync with sizes like "8x16" when user picks a preset/custom size
               var asPackId = value.ToString();
               if(!string.Equals(SelectedPackId, asPackId, StringComparison.OrdinalIgnoreCase))
                  _selectedPackId = asPackId;

               // if a glyph is currently edited, reset to new size
               EditedBitmap = null;
               IsGlyphDirty = false;
               _ = RenderAsync();
            }
         }
      }

      // Backward-compat binding used by existing controls.
      public GridSize GlyphSize => SelectedGlyphSize;

      private int _customGlyphWidth = 8;
      public int CustomGlyphWidth
      {
         get => _customGlyphWidth;
         set
         {
            if(Set(ref _customGlyphWidth, value))
               TryApplyCustomGlyphSize();
         }
      }

      private int _customGlyphHeight = 16;
      public int CustomGlyphHeight
      {
         get => _customGlyphHeight;
         set
         {
            if(Set(ref _customGlyphHeight, value))
               TryApplyCustomGlyphSize();
         }
      }
      public GlyphId FallbackGlyph { get; } = GlyphId.FromInternal("MISSING");

      public TextLayoutOptions LayoutOptions { get; } = new TextLayoutOptions
      {
         Wrap = true,
         Clip = true,
         TabWidth = 4
      };

      public AsyncCommand ReloadCommand { get; }
      public AsyncCommand RenderCommand { get; }
      public AsyncCommand OpenWorkspaceCommand { get; }
      public AsyncCommand CreateSymbolsCommand { get; }
      public AsyncCommand ImportFontCommand { get; }
      public AsyncCommand SaveGlyphCommand { get; }
      public AsyncCommand ClearGlyphCommand { get; }
      public AsyncCommand RevertGlyphCommand { get; }

      public Func<Task>? OpenWorkspaceHandler { get; set; }
      public Func<Task>? CreateSymbolsHandler { get; set; }
      public Func<Task>? ImportFontHandler { get; set; }
      public Func<Task>? GlyphSavedHandler { get; set; }
      public Func<GlyphId, GlyphBitmap, Task>? GlyphBitmapReplacedHandler { get; set; }

      private GlyphBitmap? _editedBitmap;
      public GlyphBitmap? EditedBitmap
      {
         get => _editedBitmap;
         set
         {
            if(Set(ref _editedBitmap, value))
               IsGlyphDirty = value != null;
         }
      }

      private bool _isGlyphDirty;
      public bool IsGlyphDirty
      {
         get => _isGlyphDirty;
         private set => Set(ref _isGlyphDirty, value);
      }

      public MainViewModel(IFontPackProvider packs, IGlyphRepository repo)
      {
         _packs = packs ?? throw new ArgumentNullException(nameof(packs));
         _repo = repo ?? throw new ArgumentNullException(nameof(repo));

         _resolver = new GlyphResolver(_repo);
         _renderer = new TextToTerminalRenderer(_resolver);

         UpdateSelectedGlyphSizeFromPackId(_selectedPackId);
         _customGlyphWidth = SelectedGlyphSize.Width;
         _customGlyphHeight = SelectedGlyphSize.Height;

         ReloadCommand = new AsyncCommand(ReloadAsync);
         RenderCommand = new AsyncCommand(RenderAsync);

         OpenWorkspaceCommand = new AsyncCommand(() => OpenWorkspaceHandler?.Invoke() ?? Task.CompletedTask);
         CreateSymbolsCommand = new AsyncCommand(() => CreateSymbolsHandler?.Invoke() ?? Task.CompletedTask);
         ImportFontCommand = new AsyncCommand(() => ImportFontHandler?.Invoke() ?? Task.CompletedTask);
         SaveGlyphCommand = new AsyncCommand(SaveSelectedGlyphAsync);
         ClearGlyphCommand = new AsyncCommand(ClearSelectedGlyphAsync);
         RevertGlyphCommand = new AsyncCommand(RevertSelectedGlyphAsync);
      }

      private void UpdateSelectedGlyphSizeFromPackId(string? packId)
      {
         if(string.IsNullOrWhiteSpace(packId))
            return;

         var parsed = TryParseGridSize(packId, out var size) ? size : SelectedGlyphSize;
         if(!parsed.Equals(SelectedGlyphSize))
         {
            _selectedGlyphSize = parsed;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedGlyphSize)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GlyphSize)));

            _customGlyphWidth = parsed.Width;
            _customGlyphHeight = parsed.Height;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomGlyphWidth)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomGlyphHeight)));
         }
      }

      private void TryApplyCustomGlyphSize()
      {
         if(CustomGlyphWidth <= 0 || CustomGlyphHeight <= 0)
            return;

         var next = new GridSize(CustomGlyphWidth, CustomGlyphHeight);
         if(next.Equals(SelectedGlyphSize))
            return;

         SelectedGlyphSize = next;
      }

      private static bool TryParseGridSize(string text, out GridSize size)
      {
         size = default;
         if(string.IsNullOrWhiteSpace(text)) return false;

         var parts = text.Trim().Split('x', 'X');
         if(parts.Length != 2) return false;

         if(!int.TryParse(parts[0], out var w)) return false;
         if(!int.TryParse(parts[1], out var h)) return false;
         if(w <= 0 || h <= 0) return false;

         size = new GridSize(w, h);
         return true;
      }

      public async Task SaveSelectedGlyphAsync()
      {
         if(EditedBitmap == null)
            return;

         var glyph = new Glyph(SelectedGlyph, GlyphSize, EditedBitmap.Clone());
         await _repo.SaveAsync(SelectedPackId, SelectedStyle, glyph, CancellationToken.None).ConfigureAwait(false);

         IsGlyphDirty = false;

         if(GlyphSavedHandler != null)
            await GlyphSavedHandler().ConfigureAwait(false);
      }

      public async Task ClearSelectedGlyphAsync()
      {
         var empty = new GlyphBitmap(GlyphSize, new byte[GlyphBitmap.GetByteLength(GlyphSize)]);
         EditedBitmap = empty;

         if(GlyphBitmapReplacedHandler != null)
            await GlyphBitmapReplacedHandler(SelectedGlyph, empty).ConfigureAwait(false);
      }

      public async Task RevertSelectedGlyphAsync()
      {
         GlyphBitmap bmp;
         if(await _repo.ExistsAsync(SelectedPackId, SelectedStyle, SelectedGlyph, CancellationToken.None).ConfigureAwait(false))
         {
            var glyph = await _repo.LoadAsync(SelectedPackId, SelectedStyle, SelectedGlyph, CancellationToken.None).ConfigureAwait(false);
            bmp = glyph.Bitmap.Clone();
         }
         else
         {
            bmp = new GlyphBitmap(GlyphSize, new byte[GlyphBitmap.GetByteLength(GlyphSize)]);
         }

         EditedBitmap = bmp;
         IsGlyphDirty = false;

         if(GlyphBitmapReplacedHandler != null)
            await GlyphBitmapReplacedHandler(SelectedGlyph, bmp).ConfigureAwait(false);
      }

      public async Task ReloadAsync()
      {
         PackIds.Clear();

         var list = await _packs.ListPacksAsync(CancellationToken.None).ConfigureAwait(false);
         foreach(var p in list)
            PackIds.Add(p.PackId);

         if(PackIds.Count > 0)
         {
            // Keep selection if possible
            if(!PackIds.Contains(SelectedPackId))
               SelectedPackId = PackIds[0];
         }

         await ReloadStylesAndRenderAsync().ConfigureAwait(false);
      }

      private async Task ReloadStylesAndRenderAsync()
      {
         Styles.Clear();

         if(!string.IsNullOrWhiteSpace(SelectedPackId))
         {
            var styles = await _packs.ListStylesAsync(SelectedPackId, CancellationToken.None).ConfigureAwait(false);
            foreach(var s in styles)
               Styles.Add(s);
         }

         // Ensure selection exists
         if(Styles.Count > 0)
         {
            bool found = false;
            foreach(var s in Styles)
            {
               if(s.Equals(SelectedStyle))
               {
                  found = true;
                  break;
               }
            }

            if(!found)
               SelectedStyle = Styles[0];
         }

         await RenderAsync().ConfigureAwait(false);
      }

      public async Task RenderAsync()
      {
         // Scale terminal preview size based on glyph pixel size.
         // Smaller glyphs -> more cells, bigger glyphs -> fewer cells.
         int w = 40;
         int h = 12;

         var gs = SelectedGlyphSize;
         if(gs.Width <= 6 && gs.Height <= 10)
         {
            w = 60;
            h = 18;
         }
         else if(gs.Width >= 24 || gs.Height >= 24)
         {
            w = 24;
            h = 10;
         }

         var buffer = await _renderer.RenderAsync(
             SelectedPackId,
             SelectedStyle,
             Text,
             w,
             h,
             FallbackGlyph,
             LayoutOptions,
             CancellationToken.None
         ).ConfigureAwait(false);

         PreviewBuffer = buffer;
      }

      // --- Helper: ruční oznámení změny odvozených vlastností ---
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
