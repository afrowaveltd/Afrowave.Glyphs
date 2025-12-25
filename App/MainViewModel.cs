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
         // LCD Character Displays (HD44780, OLED modules)
         new GridSize(5, 7),    // Tiny 5x7 (classic LCD)
         new GridSize(5, 8),    // Standard LCD character
         new GridSize(6, 8),    // 6x8 LCD
         new GridSize(5, 10),   // Taller LCD variant

         // Classic computer fonts
         new GridSize(7, 9),    // CGA/EGA
         new GridSize(8, 8),    // Square 8x8 (C64, ZX Spectrum)
         new GridSize(8, 14),   // VGA text mode
         new GridSize(8, 16),   // Standard VGA/BIOS font
         new GridSize(9, 16),   // VGA 9-wide variant

         // Modern terminal / editor fonts
         new GridSize(10, 20),  // 2x scale of 5x10
         new GridSize(12, 16),  // Wide proportional
         new GridSize(16, 16),  // Square pixel art
         new GridSize(16, 32),  // Tall, detailed

         // High resolution / pixel art
         new GridSize(24, 24),  // Medium detail
         new GridSize(32, 32),  // High detail
         new GridSize(48, 48),  // Very high detail
         new GridSize(64, 64),  // Ultra detail
      };

      private string _selectedPackId = "8x16";
      public string SelectedPackId
      {
         get => _selectedPackId;
         set
         {
            // Ensure the pack is in the collection (in case it's a new size)
            if(!string.IsNullOrWhiteSpace(value) && !PackIds.Contains(value))
            {
               PackIds.Add(value);
            }

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
            // Ignore attempts to set default/empty struct value (happens when ComboBox loses selection)
            if(string.IsNullOrEmpty(value.Name))
               return;

            if(Set(ref _selectedStyle, value))
               _ = RenderAsync();
         }
      }

      private GlyphId _selectedGlyph = GlyphId.FromUnicode('H');
      public GlyphId SelectedGlyph
      {
         get => _selectedGlyph;
         set
         {
            if(Set(ref _selectedGlyph, value))
            {
               // SelectedGlyphText je odvozená vlastnost, musíme ji oznámit ručně
               Notify(nameof(SelectedGlyphText));

               // Reset EditedBitmap when glyph changes, so preview loads fresh from cache
               EditedBitmap = null;
            }
         }
      }

      public string SelectedGlyphText => _selectedGlyph.ToString();

      private string _text = "A: \\u0041\r\nB: \\u0042\r\nCurrent editing: {current}";
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
               // Update custom width/height fields to match
               _customGlyphWidth = value.Width;
               _customGlyphHeight = value.Height;
               Notify(nameof(CustomGlyphWidth));
               Notify(nameof(CustomGlyphHeight));
               Notify(nameof(GlyphSize)); // IMPORTANT: Notify GlyphSize for backward compatibility

               // keep pack id in sync with sizes like "8x16" when user picks a preset/custom size
               var asPackId = value.ToString();
               if(!string.Equals(SelectedPackId, asPackId, StringComparison.OrdinalIgnoreCase))
               {
                  // Use the public property to trigger ReloadAsync and property change
                  SelectedPackId = asPackId;
               }

               // Always reset edited bitmap when size changes
               EditedBitmap = null;
               IsGlyphDirty = false;

               // Clear cache when size changes so preview reloads glyphs with new size
               if(GlyphSavedHandler != null)
                  _ = GlyphSavedHandler(); // This will clear cache and re-render
               else
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
      public AsyncCommand CreatePackCommand { get; }
      public AsyncCommand CreateStyleCommand { get; }
      public AsyncCommand SaveGlyphCommand { get; }
      public AsyncCommand ClearGlyphCommand { get; }
      public AsyncCommand RevertGlyphCommand { get; }
      public AsyncCommand CloneGlyphCommand { get; }

      public Func<Task>? OpenWorkspaceHandler { get; set; }
      public Func<Task>? CreateSymbolsHandler { get; set; }
      public Func<Task>? ImportFontHandler { get; set; }
      public Func<Task>? CreatePackHandler { get; set; }
      public Func<Task>? CreateStyleHandler { get; set; }
      public Func<Task>? GlyphSavedHandler { get; set; }
      public Func<GlyphId, GlyphBitmap, Task>? GlyphBitmapReplacedHandler { get; set; }
      public Func<Task>? CloneGlyphHandler { get; set; }
      public Func<Task>? UnicodeRangeWizardHandler { get; set; }

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
         CreatePackCommand = new AsyncCommand(() => CreatePackHandler?.Invoke() ?? Task.CompletedTask);
         CreateStyleCommand = new AsyncCommand(() => CreateStyleHandler?.Invoke() ?? Task.CompletedTask);
         SaveGlyphCommand = new AsyncCommand(SaveSelectedGlyphAsync);
         ClearGlyphCommand = new AsyncCommand(ClearSelectedGlyphAsync);
         RevertGlyphCommand = new AsyncCommand(RevertSelectedGlyphAsync);
         CloneGlyphCommand = new AsyncCommand(() => CloneGlyphHandler?.Invoke() ?? Task.CompletedTask);
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
         System.Diagnostics.Debug.WriteLine($"[RELOAD] ListPacksAsync returned {list.Count} packs");

         foreach(var p in list)
         {
            System.Diagnostics.Debug.WriteLine($"[RELOAD] Adding pack: {p.PackId}");
            PackIds.Add(p.PackId);
         }

         System.Diagnostics.Debug.WriteLine($"[RELOAD] PackIds.Count = {PackIds.Count}");

         if(PackIds.Count > 0)
         {
            // Keep selection if possible
            if(!PackIds.Contains(SelectedPackId))
               SelectedPackId = PackIds[0];
         }

         await ReloadStylesAndRenderAsync().ConfigureAwait(false);

         // Clear cache to reload glyphs from disk
         if(GlyphSavedHandler != null)
            await GlyphSavedHandler().ConfigureAwait(false);

         // Auto-select first character of text after successful load
         if(!string.IsNullOrEmpty(Text))
         {
            SelectedGlyph = GlyphId.FromUnicode((int)Text[0]);
         }
      }

      private async Task ReloadStylesAndRenderAsync()
      {
         var previousStyle = SelectedStyle;

         if(!string.IsNullOrWhiteSpace(SelectedPackId))
         {
            var styles = await _packs.ListStylesAsync(SelectedPackId, CancellationToken.None).ConfigureAwait(false);

            // If no styles returned, ensure we have at least _base_
            if(styles.Count == 0)
            {
               var defaultStyle = new FontStyleId("_base_");
               // Add to collection
               if(Styles.Count == 0 || !Styles.Contains(defaultStyle))
               {
                  Styles.Clear();
                  Styles.Add(defaultStyle);
               }
            }
            else
            {
               // Update collection without clearing to avoid null binding issues
               // Remove styles that are no longer present
               for(int i = Styles.Count - 1; i >= 0; i--)
               {
                  bool found = false;
                  foreach(var s in styles)
                  {
                     if(Styles[i].Equals(s))
                     {
                        found = true;
                        break;
                     }
                  }
                  if(!found)
                     Styles.RemoveAt(i);
               }

               // Add new styles
               foreach(var s in styles)
               {
                  bool found = false;
                  foreach(var existing in Styles)
                  {
                     if(existing.Equals(s))
                     {
                        found = true;
                        break;
                     }
                  }
                  if(!found)
                     Styles.Add(s);
               }
            }
         }
         else
         {
            // If no pack selected, ensure we have at least _base_
            if(Styles.Count == 0)
            {
               Styles.Add(new FontStyleId("_base_"));
            }
         }

         // Ensure we always have at least one style
         if(Styles.Count == 0)
         {
            Styles.Add(new FontStyleId("_base_"));
         }

         // Ensure selection exists
         if(Styles.Count > 0)
         {
            bool found = false;

            // Only try to restore previous style if it has a valid name
            if(!string.IsNullOrEmpty(previousStyle.Name))
            {
               foreach(var s in Styles)
               {
                  if(s.Equals(previousStyle))
                  {
                     found = true;
                     break;
                  }
               }
            }

            if(!found)
            {
               // Select first style (should be _base_)
               SelectedStyle = Styles[0];
            }
            else if(!SelectedStyle.Equals(previousStyle))
            {
               SelectedStyle = previousStyle;
            }
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

         // Process UTF escape sequences in preview text
         string processedText = ProcessUtfEscapes(Text);

         // Add current edited glyph preview if EditedBitmap exists
         if(EditedBitmap != null && SelectedGlyph.IsUnicode && SelectedGlyph.UnicodeCodePoint.HasValue)
         {
            // Temporarily save edited bitmap to repository cache so renderer can find it
            var tempGlyph = new Glyph(SelectedGlyph, GlyphSize, EditedBitmap.Clone());
            await _repo.SaveAsync(SelectedPackId, SelectedStyle, tempGlyph, CancellationToken.None).ConfigureAwait(false);
         }

         var buffer = await _renderer.RenderAsync(
             SelectedPackId,
             SelectedStyle,
             processedText,
             w,
             h,
             FallbackGlyph,
             LayoutOptions,
             CancellationToken.None
         ).ConfigureAwait(false);

         PreviewBuffer = buffer;
      }

      private string ProcessUtfEscapes(string input)
      {
         if(string.IsNullOrEmpty(input))
            return input;

         var result = input;

         // {current} -> Currently edited glyph
         if(result.Contains("{current}"))
         {
            char currentChar = SelectedGlyph.IsUnicode && SelectedGlyph.UnicodeCodePoint.HasValue
               ? (char)SelectedGlyph.UnicodeCodePoint.Value
               : '?';
            result = result.Replace("{current}", currentChar.ToString());
         }

         // \uXXXX -> Unicode character (4 hex digits)
         result = System.Text.RegularExpressions.Regex.Replace(result, @"\\u([0-9A-Fa-f]{4})", m =>
         {
            int codePoint = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            return char.ConvertFromUtf32(codePoint);
         });

         // \U+XXXX or {U+XXXX} -> Unicode character (flexible hex)
         result = System.Text.RegularExpressions.Regex.Replace(result, @"(?:\\U\+|{U\+)([0-9A-Fa-f]+)\}?", m =>
         {
            int codePoint = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            return char.ConvertFromUtf32(codePoint);
         });

         return result;
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
