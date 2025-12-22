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

      public event PropertyChangedEventHandler? PropertyChanged;

      public ObservableCollection<string> PackIds { get; } = new ObservableCollection<string>();
      public ObservableCollection<FontStyleId> Styles { get; } = new ObservableCollection<FontStyleId>();

      private string _selectedPackId = "8x16";
      public string SelectedPackId
      {
         get => _selectedPackId;
         set
         {
            if(Set(ref _selectedPackId, value))
               _ = ReloadStylesAndRenderAsync();
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

      private GlyphId _selectedGlyph;
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

      public GridSize GlyphSize { get; } = new GridSize(8, 16);
      public GlyphId FallbackGlyph { get; } = GlyphId.FromInternal("MISSING");

      public TextLayoutOptions LayoutOptions { get; } = new TextLayoutOptions
      {
         Wrap = true,
         Clip = true,
         TabWidth = 4
      };

      public AsyncCommand ReloadCommand { get; }
      public AsyncCommand RenderCommand { get; }

      public MainViewModel(IFontPackProvider packs, IGlyphRepository repo)
      {
         _packs = packs ?? throw new ArgumentNullException(nameof(packs));
         _repo = repo ?? throw new ArgumentNullException(nameof(repo));

         _resolver = new GlyphResolver(_repo);
         _renderer = new TextToTerminalRenderer(_resolver);

         ReloadCommand = new AsyncCommand(ReloadAsync);
         RenderCommand = new AsyncCommand(RenderAsync);
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
         // MVP: fixed terminal size
         const int w = 40;
         const int h = 12;

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
