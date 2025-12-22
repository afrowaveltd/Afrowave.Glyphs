using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Core.Models;
using Core.Text;
using Editor.Avalonia.Services;
using Storage.Abstractions.Models;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Editor.Avalonia;

public partial class TerminalPreviewControl : UserControl
{
   public static readonly StyledProperty<TerminalBuffer> BufferProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, TerminalBuffer>(nameof(Buffer));

   public static readonly StyledProperty<GridSize> GlyphSizeProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, GridSize>(nameof(GlyphSize), new GridSize(8, 16));

   public static readonly StyledProperty<int> PixelScaleProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, int>(nameof(PixelScale), 2);

   public static readonly StyledProperty<bool> ShowGridProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, bool>(nameof(ShowGrid), true);

   public static readonly StyledProperty<bool> HighlightFallbackProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, bool>(nameof(HighlightFallback), true);

   public static readonly StyledProperty<string> PackIdProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, string>(nameof(PackId), "8x16");

   public static readonly StyledProperty<FontStyleId> StyleProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, FontStyleId>(nameof(Style), new FontStyleId("_base_"));

   public static readonly StyledProperty<GlyphId> FallbackGlyphProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, GlyphId>(nameof(FallbackGlyph), GlyphId.FromInternal("MISSING"));

   public static readonly StyledProperty<GlyphId> SelectedGlyphProperty =
       AvaloniaProperty.Register<TerminalPreviewControl, GlyphId>(nameof(SelectedGlyph));

   private readonly Canvas _canvas;
   private TerminalGlyphCache? _cache;
   public event Action<GlyphId>? GlyphClicked;


   public TerminalPreviewControl()
   {
      InitializeComponent();
      _canvas = this.FindControl<Canvas>("PART_Canvas")!; // Přidáno ! pro potlašení CS8601
      this.AddHandler(
    PointerPressedEvent,
    (sender, e) => OnPointerPressed(sender, (PointerPressedEventArgs)e),
    RoutingStrategies.Bubble
);





      this.GetObservable(BufferProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(GlyphSizeProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(PixelScaleProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(ShowGridProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(HighlightFallbackProperty).Subscribe(_ => RequestRedraw());

   }

   private void RequestRedraw()
   {
      _ = RedrawAsync();
   }

   public TerminalBuffer Buffer
   {
      get => GetValue(BufferProperty);
      set => SetValue(BufferProperty, value);
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

   public bool ShowGrid
   {
      get => GetValue(ShowGridProperty);
      set => SetValue(ShowGridProperty, value);
   }

   public bool HighlightFallback
   {
      get => GetValue(HighlightFallbackProperty);
      set => SetValue(HighlightFallbackProperty, value);
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

   private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
   {
      if(_cache == null || Buffer == null)
         return;

      var point = e.GetPosition(_canvas);

      int cellWidth = GlyphSize.Width * PixelScale;
      int cellHeight = GlyphSize.Height * PixelScale;

      int cellX = (int)(point.X / cellWidth);
      int cellY = (int)(point.Y / cellHeight);

      if(cellX < 0 || cellY < 0)
         return;

      if(cellX >= Buffer.Width || cellY >= Buffer.Height)
         return;

      var cell = Buffer.GetCell(cellX, cellY);
      if(cell == null) return;

      var glyphId = cell.Requested;
      SelectedGlyph = glyphId;
      GlyphClicked?.Invoke(glyphId);

      if(cell.IsFallback)
      {
         // třeba zvýraznit červeně
      }

      CellClicked?.Invoke(cellX, cellY);

   }
   public event Action<int, int>? CellClicked;


   public GlyphId FallbackGlyph
   {
      get => GetValue(FallbackGlyphProperty);
      set => SetValue(FallbackGlyphProperty, value);
   }

   public GlyphId SelectedGlyph
   {
      get => GetValue(SelectedGlyphProperty);
      set => SetValue(SelectedGlyphProperty, value);
   }

   // Call this from the host (window/viewmodel) after DI creates cache.
   public void SetCache(TerminalGlyphCache cache)
   {
      _cache = cache;
      _ = RedrawAsync();
   }

   private async Task RedrawAsync()
   {
      if(_cache == null) return;
      if(_canvas == null) return;

      var buffer = Buffer;
      if(buffer == null) return;

      _canvas.Children.Clear();

      int scale = PixelScale <= 0 ? 1 : PixelScale;
      var gsize = GlyphSize;

      int cellPixelW = gsize.Width * scale;
      int cellPixelH = gsize.Height * scale;

      _canvas.Width = buffer.Width * cellPixelW;
      _canvas.Height = buffer.Height * cellPixelH;

      // Grid lines (optional)
      if(ShowGrid)
      {
         for(int cx = 0; cx <= buffer.Width; cx++)
         {
            var line = new global::Avalonia.Controls.Shapes.Line
            {
               StartPoint = new Point(cx * cellPixelW, 0),
               EndPoint = new Point(cx * cellPixelW, buffer.Height * cellPixelH),
               Stroke = Brushes.Gray,
               StrokeThickness = 1,
               Opacity = 0.2
            };
            _canvas.Children.Add(line);
         }

         for(int cy = 0; cy <= buffer.Height; cy++)
         {
            var line = new global::Avalonia.Controls.Shapes.Line
            {
               StartPoint = new Point(0, cy * cellPixelH),
               EndPoint = new Point(buffer.Width * cellPixelW, cy * cellPixelH),
               Stroke = Brushes.Gray,
               StrokeThickness = 1,
               Opacity = 0.2
            };
            _canvas.Children.Add(line);
         }
      }

      // Render glyphs
      for(int y = 0; y < buffer.Height; y++)
      {
         for(int x = 0; x < buffer.Width; x++)
         {
            var cell = buffer.GetCell(x, y);
            if(cell == null) continue;

            var bmp = await _cache.GetBitmapAsync(PackId, Style, cell.Resolved, gsize, FallbackGlyph, CancellationToken.None)
                .ConfigureAwait(true); // UI thread ok for MVP

            DrawGlyphBitmap(x, y, bmp, scale, cellPixelW, cellPixelH);

            if(HighlightFallback && cell.IsFallback)
            {
               var border = new global::Avalonia.Controls.Border
               {
                  Width = cellPixelW,
                  Height = cellPixelH,
                  BorderBrush = Brushes.Orange,
                  BorderThickness = new Thickness(1),
                  Opacity = 0.6
               };
               Canvas.SetLeft(border, x * cellPixelW);
               Canvas.SetTop(border, y * cellPixelH);
               _canvas.Children.Add(border);
            }
         }
      }
   }

   private void DrawGlyphBitmap(int cellX, int cellY, GlyphBitmap bitmap, int scale, int cellPixelW, int cellPixelH)
   {
      // Draw ON pixels as rectangles
      int offsetX = cellX * cellPixelW;
      int offsetY = cellY * cellPixelH;

      for(int py = 0; py < bitmap.Size.Height; py++)
      {
         for(int px = 0; px < bitmap.Size.Width; px++)
         {
            if(!bitmap.GetPixel(px, py))
               continue;

            var rect = new global::Avalonia.Controls.Shapes.Rectangle
            {
               Width = scale,
               Height = scale,
               Fill = Brushes.White
            };

            Canvas.SetLeft(rect, offsetX + px * scale);
            Canvas.SetTop(rect, offsetY + py * scale);
            _canvas.Children.Add(rect);
         }
      }
   }
}