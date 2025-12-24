using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Core.Models;
using Editor.Avalonia.Services;
using Storage.Abstractions.Models;
using System; // Přidat using pro Action
using System.Threading;
using System.Threading.Tasks;

namespace Editor.Avalonia.Controls;

public partial class GlyphPreviewControl : UserControl
{
   public static readonly StyledProperty<GlyphId> GlyphIdProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, GlyphId>(nameof(GlyphId));

   public static readonly StyledProperty<GridSize> GlyphSizeProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, GridSize>(nameof(GlyphSize), new GridSize(8, 16));

   public static readonly StyledProperty<int> PixelScaleProperty =
       AvaloniaProperty.Register<GlyphPreviewControl, int>(nameof(PixelScale), 12);

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

   private void RequestRedraw() => _ = RedrawAsync();

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
      int scale = PixelScale <= 0 ? 10 : PixelScale;

      _canvas.Children.Clear();
      _canvas.Width = gsize.Width * scale;
      _canvas.Height = gsize.Height * scale;

      // Draw pixel grid behind glyph pixels (thin, subtle).
      var gridBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
      for(int x = 0; x <= gsize.Width; x++)
      {
         var line = new Line
         {
            StartPoint = new Point(x * scale + 0.5, 0),
            EndPoint = new Point(x * scale + 0.5, gsize.Height * scale),
            Stroke = gridBrush,
            StrokeThickness = 1,
            IsHitTestVisible = false
         };
         _canvas.Children.Add(line);
      }

      for(int y = 0; y <= gsize.Height; y++)
      {
         var line = new Line
         {
            StartPoint = new Point(0, y * scale + 0.5),
            EndPoint = new Point(gsize.Width * scale, y * scale + 0.5),
            Stroke = gridBrush,
            StrokeThickness = 1,
            IsHitTestVisible = false
         };
         _canvas.Children.Add(line);
      }

      // Keep an editable clone for the currently selected glyph.
      if(_editable == null || !_editableId.Equals(GlyphId) || !_editable.Size.Equals(gsize))
      {
         var loaded = await _cache.GetBitmapAsync(PackId, Style, GlyphId, gsize, FallbackGlyph, CancellationToken.None);
         _editable = loaded.Clone();
         _editableId = GlyphId;
      }

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
   }

   private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
   {
      if(_editable == null) return;

      var point = e.GetPosition(_canvas);
      int scale = PixelScale <= 0 ? 10 : PixelScale;

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
