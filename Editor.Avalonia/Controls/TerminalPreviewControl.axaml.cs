using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
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
   public event Action<int, int>? CellClicked;

   private int _selectedCellX = -1;
   private int _selectedCellY = -1;

   public TerminalPreviewControl()
   {
      InitializeComponent();

      _canvas = this.FindControl<Canvas>("PART_Canvas")!;

      // PointerPressed on the control (bubble)
      AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);

      // Redraw on property changes
      this.GetObservable(BufferProperty).Subscribe(_ => { ClearSelection(); RequestRedraw(); });
      this.GetObservable(GlyphSizeProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(PixelScaleProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(ShowGridProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(HighlightFallbackProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(PackIdProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(StyleProperty).Subscribe(_ => RequestRedraw());
   }

   public void ClearSelection()
   {
      _selectedCellX = -1;
      _selectedCellY = -1;
      RequestRedraw();
   }

   private void RequestRedraw()
   {
      if(Dispatcher.UIThread.CheckAccess())
         _ = RedrawAsync();
      else
         Dispatcher.UIThread.Post(() => _ = RedrawAsync());
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
      RequestRedraw();
   }

   private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
   {
      if(_cache == null)
         return;

      var buffer = Buffer;
      if(buffer == null)
         return;

      var point = e.GetPosition(_canvas);

      int cellWidth = GlyphSize.Width * (PixelScale <= 0 ? 1 : PixelScale);
      int cellHeight = GlyphSize.Height * (PixelScale <= 0 ? 1 : PixelScale);

      int cellX = (int)(point.X / cellWidth);
      int cellY = (int)(point.Y / cellHeight);

      if(cellX < 0 || cellY < 0)
         return;

      if(cellX >= buffer.Width || cellY >= buffer.Height)
         return;

      var cell = buffer.GetCell(cellX, cellY);
      if(cell == null)
         return;

      // selection highlight
      _selectedCellX = cellX;
      _selectedCellY = cellY;

      // propagate selection
      var glyphId = cell.Requested;
      SelectedGlyph = glyphId;
      GlyphClicked?.Invoke(glyphId);
      CellClicked?.Invoke(cellX, cellY);

      RequestRedraw();
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
               Opacity = 0.2,
               IsHitTestVisible = false
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
               Opacity = 0.2,
               IsHitTestVisible = false
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

            var bmp = await _cache
               .GetBitmapAsync(PackId, Style, cell.Resolved, gsize, FallbackGlyph, CancellationToken.None)
               .ConfigureAwait(true); // UI thread ok for MVP

            DrawGlyphBitmap(x, y, bmp, scale, cellPixelW, cellPixelH);

            // fallback hint
            if(HighlightFallback && cell.IsFallback)
            {
               var fb = new global::Avalonia.Controls.Shapes.Rectangle
               {
                  Width = cellPixelW,
                  Height = cellPixelH,
                  Stroke = Brushes.Orange,
                  StrokeThickness = 1,
                  Opacity = 0.6,
                  Fill = null,
                  IsHitTestVisible = false
               };

               Canvas.SetLeft(fb, x * cellPixelW);
               Canvas.SetTop(fb, y * cellPixelH);
               _canvas.Children.Add(fb);
            }

            // selection highlight (draw last so it stays on top)
            if(x == _selectedCellX && y == _selectedCellY)
            {
               var sel = new global::Avalonia.Controls.Shapes.Rectangle
               {
                  Width = cellPixelW,
                  Height = cellPixelH,
                  Stroke = Brushes.Orange,
                  StrokeThickness = 2,
                  Opacity = 1.0,
                  Fill = null,
                  IsHitTestVisible = false
               };

               Canvas.SetLeft(sel, x * cellPixelW);
               Canvas.SetTop(sel, y * cellPixelH);
               _canvas.Children.Add(sel);
            }
         }
      }
   }

   private void DrawGlyphBitmap(int cellX, int cellY, GlyphBitmap bitmap, int scale, int cellPixelW, int cellPixelH)
   {
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
               Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)), // Bright green (Hercules style)
               IsHitTestVisible = false
            };

            Canvas.SetLeft(rect, offsetX + px * scale);
            Canvas.SetTop(rect, offsetY + py * scale);
            _canvas.Children.Add(rect);
         }
      }
   }
}
