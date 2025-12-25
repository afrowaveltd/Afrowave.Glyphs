using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Core.Models;
using Editor.Consolonia.Services;
using Storage.Abstractions.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Editor.Consolonia.Controls;

public partial class GlyphPreviewControl : UserControl
{
   public static readonly StyledProperty<GlyphId> GlyphIdProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, GlyphId>(nameof(GlyphId));

   public static readonly StyledProperty<GridSize> GlyphSizeProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, GridSize>(nameof(GlyphSize), new GridSize(8, 16));

   public static readonly StyledProperty<int> PixelScaleProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, int>(nameof(PixelScale), 1);

   public static readonly StyledProperty<string> PackIdProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, string>(nameof(PackId), "8x16");

   public static readonly StyledProperty<FontStyleId> StyleProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, FontStyleId>(nameof(Style), new FontStyleId("_base_"));

   public static readonly StyledProperty<GlyphId> FallbackGlyphProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, GlyphId>(nameof(FallbackGlyph), GlyphId.FromInternal("MISSING"));

   private Canvas _canvas = null!;
   private TerminalGlyphCache? _cache;

   private GlyphBitmap? _editable;
   private GlyphId _editableId;

   public event Action<GlyphId, GlyphBitmap>? GlyphEdited;

   public GlyphPreviewControl()
   {
      InitializeComponent();
      _canvas = this.FindControl<Canvas>("PART_Canvas")!;

      AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);

      this.GetObservable(GlyphIdProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(GlyphSizeProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(PixelScaleProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(PackIdProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(StyleProperty).Subscribe(_ => RequestRedraw());
   }

   private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

   public void SetCache(TerminalGlyphCache cache)
   {
      _cache = cache;
      RequestRedraw();
   }

   public GlyphId GlyphId
   {
      get => GetValue(GlyphIdProperty);
      set => SetValue(GlyphIdProperty, value);
   }

   public GridSize GlyphSize
   {
      get => GetValue(GlyphSizeProperty);
      set => SetValue(GlyphSizeProperty, value);
   }

   public int PixelScale
   {
      get => GetValue(PixelScaleProperty);
      set => SetValue(PixelScaleProperty, value);
   }

   public string PackId
   {
      get => GetValue(PackIdProperty);
      set => SetValue(PackIdProperty, value);
   }

   public FontStyleId Style
   {
      get => GetValue(StyleProperty);
      set => SetValue(StyleProperty, value);
   }

   public GlyphId FallbackGlyph
   {
      get => GetValue(FallbackGlyphProperty);
      set => SetValue(FallbackGlyphProperty, value);
   }

   private void RequestRedraw()
   {
      if(Dispatcher.UIThread.CheckAccess())
         _ = RedrawAsync();
      else
         Dispatcher.UIThread.Post(() => _ = RedrawAsync());
   }

   public GlyphBitmap? EditedBitmap => _editable;

   public void ReplaceEditedBitmap(GlyphId id, GlyphBitmap bitmap)
   {
      if(bitmap == null) throw new ArgumentNullException(nameof(bitmap));

      _editableId = id;
      _editable = bitmap.Clone();
      RequestRedraw();
   }

   private async Task RedrawAsync()
   {
      if(_cache == null) return;

      var gsize = GlyphSize;
      int scale = PixelScale <= 0 ? 1 : PixelScale;
      var packId = PackId;
      var style = Style;
      var glyphId = GlyphId;
      var fallbackGlyph = FallbackGlyph;

      // Always load from cache if glyph ID changed or if we don't have a bitmap
      GlyphBitmap? loaded = null;
      if(_editable == null || !_editableId.Equals(glyphId) || !_editable.Size.Equals(gsize))
         loaded = await _cache.GetBitmapAsync(packId, style, glyphId, gsize, fallbackGlyph, CancellationToken.None);

      await Dispatcher.UIThread.InvokeAsync(() =>
      {
         if(loaded != null)
         {
            _editable = loaded.Clone();
            _editableId = glyphId;
         }

         // If still null after load attempt, create empty bitmap
         if(_editable == null)
         {
            _editable = new GlyphBitmap(gsize, new byte[GlyphBitmap.GetByteLength(gsize)]);
            _editableId = glyphId;
         }

         _canvas.Children.Clear();
         _canvas.Width = gsize.Width * scale;
         _canvas.Height = gsize.Height * scale;

         var bmp = _editable;
         if(bmp == null)
            return;

         for(int y = 0; y < bmp.Size.Height; y++)
         {
            for(int x = 0; x < bmp.Size.Width; x++)
            {
               if(!bmp.GetPixel(x, y))
                  continue;

               var rect = new Rectangle
               {
                  Width = scale,
                  Height = scale,
                  Fill = Brushes.White,
                  IsHitTestVisible = false
               };

               Canvas.SetLeft(rect, x * scale);
               Canvas.SetTop(rect, y * scale);
               _canvas.Children.Add(rect);
            }
         }
      });
   }

   private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
   {
      if(_editable == null) return;

      var point = e.GetPosition(_canvas);
      int scale = PixelScale <= 0 ? 1 : PixelScale;

      int x = (int)(point.X / scale);
      int y = (int)(point.Y / scale);

      if(x < 0 || y < 0 || x >= _editable.Size.Width || y >= _editable.Size.Height)
         return;

      bool on = _editable.GetPixel(x, y);
      _editable.SetPixel(x, y, !on);

      GlyphEdited?.Invoke(GlyphId, _editable);
      RequestRedraw();
   }
}
