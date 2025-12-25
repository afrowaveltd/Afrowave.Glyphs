using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Core.Models;
using Tools.Services;
using Storage.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Editor.Avalonia.Controls;

public enum DrawTool
{
   Pencil,   // Free-hand drawing
   Line,     // Straight line between two points
   Rectangle, // Rectangle (future)
   Circle,   // Circle (future)
   Fill      // Flood fill (future)
}

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
   private bool _isDrawing = false;
   private bool _drawValue = true; // true = paint, false = erase

   // Tool state
   private DrawTool _currentTool = DrawTool.Pencil;
   private Point? _lineStart = null;
   private GlyphBitmap? _beforeLineSnapshot = null; // Snapshot before drawing line preview

   private Point? _rectStart = null;
   private GlyphBitmap? _beforeRectSnapshot = null; // Snapshot before drawing rectangle preview

   private Point? _circleCenter = null;
   private GlyphBitmap? _beforeCircleSnapshot = null; // Snapshot before drawing circle preview

   // Undo/Redo system
   private readonly Stack<GlyphBitmap> _undoStack = new Stack<GlyphBitmap>();
   private readonly Stack<GlyphBitmap> _redoStack = new Stack<GlyphBitmap>();
   private const int MAX_UNDO_LEVELS = 50; // Limit history to prevent memory issues

   public event Action<GlyphId, GlyphBitmap>? GlyphEdited;
   public event Action? UndoRedoStateChanged; // Notify UI when undo/redo availability changes
   public event Action<DrawTool>? CurrentToolChanged; // Notify UI when active tool changes

   public bool CanUndo => _undoStack.Count > 0;
   public bool CanRedo => _redoStack.Count > 0;

   public GlyphPreviewControl()
   {
      InitializeComponent();
      _canvas = this.FindControl<Canvas>("PART_Canvas")!;

      AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
      AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble);
      AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble);
      AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);

      this.GetObservable(GlyphIdProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(GlyphSizeProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(PixelScaleProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(PackIdProperty).Subscribe(_ => RequestRedraw());
      this.GetObservable(StyleProperty).Subscribe(_ => RequestRedraw());

      Focusable = true; // Enable keyboard input
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
      => Dispatcher.UIThread.Post(() => _ = RedrawAsync());

   public GlyphBitmap? EditedBitmap => _editable;

   public DrawTool CurrentTool
   {
      get => _currentTool;
      set
      {
         if(_currentTool != value)
         {
            _currentTool = value;
            // Reset tool state when changing tools
            _lineStart = null;
            _beforeLineSnapshot = null;
            _rectStart = null;
            _beforeRectSnapshot = null;
            _circleCenter = null;
            _beforeCircleSnapshot = null;

            // Notify UI about tool change
            CurrentToolChanged?.Invoke(_currentTool);
         }
      }
   }

   private void SaveSnapshot()
   {
      if(_editable == null) return;

      // Push current state to undo stack
      _undoStack.Push(_editable.Clone());

      // Limit stack size
      if(_undoStack.Count > MAX_UNDO_LEVELS)
      {
         // Remove oldest snapshot (bottom of stack)
         var temp = new Stack<GlyphBitmap>(_undoStack.Reverse().Skip(1).Reverse());
         _undoStack.Clear();
         foreach(var item in temp)
            _undoStack.Push(item);
      }

      // Clear redo stack when new change is made
      _redoStack.Clear();

      UndoRedoStateChanged?.Invoke();
   }

   public void Undo()
   {
      if(_editable == null || !CanUndo) return;

      // Save current state to redo stack
      _redoStack.Push(_editable.Clone());

      // Restore previous state
      _editable = _undoStack.Pop().Clone();

      GlyphEdited?.Invoke(GlyphId, _editable);
      RequestRedraw();
      UndoRedoStateChanged?.Invoke();
   }

   public void Redo()
   {
      if(_editable == null || !CanRedo) return;

      // Save current state to undo stack
      _undoStack.Push(_editable.Clone());

      // Restore next state
      _editable = _redoStack.Pop().Clone();

      GlyphEdited?.Invoke(GlyphId, _editable);
      RequestRedraw();
      UndoRedoStateChanged?.Invoke();
   }

   public void ClearHistory()
   {
      _undoStack.Clear();
      _redoStack.Clear();
      UndoRedoStateChanged?.Invoke();
   }

   public void ReplaceEditedBitmap(GlyphId id, GlyphBitmap bitmap)
   {
      if(bitmap == null) throw new ArgumentNullException(nameof(bitmap));

      _editableId = id;
      _editable = bitmap.Clone();
      ClearHistory(); // Clear undo/redo when loading new glyph
      RequestRedraw();
   }

   private async Task RedrawAsync()
   {
      if(_cache == null) return;

      var gsize = GlyphSize;
      int scale = PixelScale <= 0 ? 10 : PixelScale;
      var packId = PackId;
      var style = Style;
      var glyphId = GlyphId;
      var fallbackGlyph = FallbackGlyph;

      // Always load from cache if glyph ID changed or if we don't have a bitmap or size changed
      GlyphBitmap? loaded = null;
      bool sizeChanged = _editable != null && !_editable.Size.Equals(gsize);

      if(_editable == null || !_editableId.Equals(glyphId) || sizeChanged)
      {
         loaded = await _cache.GetBitmapAsync(packId, style, glyphId, gsize, fallbackGlyph, CancellationToken.None);
      }

      await Dispatcher.UIThread.InvokeAsync(() =>
      {
         if(loaded != null)
         {
            _editable = loaded.Clone();
            _editableId = glyphId;
         }
         else if(sizeChanged || _editable == null)
         {
            // Size changed or no bitmap - create empty bitmap with new size
            _editable = new GlyphBitmap(gsize, new byte[GlyphBitmap.GetByteLength(gsize)]);
            _editableId = glyphId;
         }

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

         private void OnKeyDown(object? sender, KeyEventArgs e)
         {
            // Undo/Redo shortcuts
            if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
               if(e.Key == Key.Z)
               {
                  Undo();
                  e.Handled = true;
                  return;
               }
               else if(e.Key == Key.Y)
               {
                  Redo();
                  e.Handled = true;
                  return;
               }
            }

            // Tool selection shortcuts
            switch(e.Key)
            {
               case Key.P:
                  CurrentTool = DrawTool.Pencil;
                  e.Handled = true;
                  break;
               case Key.L:
                  CurrentTool = DrawTool.Line;
                  e.Handled = true;
                  break;
               case Key.R:
                  CurrentTool = DrawTool.Rectangle;
                  e.Handled = true;
                  break;
               case Key.C:
                  CurrentTool = DrawTool.Circle;
                  e.Handled = true;
                  break;
               case Key.F:
                  CurrentTool = DrawTool.Fill;
                  e.Handled = true;
                  break;
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

               if(_currentTool == DrawTool.Pencil)
               {
                  // Start drawing
                  _isDrawing = true;
                  bool currentValue = _editable.GetPixel(x, y);
                  _drawValue = !currentValue; // Toggle: if it's ON, we'll erase (false). If OFF, we'll paint (true).

                  SaveSnapshot(); // Save state before editing

                  _editable.SetPixel(x, y, _drawValue);

                  GlyphEdited?.Invoke(GlyphId, _editable);
                  RequestRedraw();
               }
               else if(_currentTool == DrawTool.Line)
               {
                  if(_lineStart == null)
                  {
                     // First click - set start point
                     _lineStart = new Point(x, y);
                     _beforeLineSnapshot = _editable.Clone();
                     _drawValue = !_editable.GetPixel(x, y); // Decide paint or erase based on first pixel
                  }
                     else
                     {
                        // Second click - draw final line and reset
                        SaveSnapshot(); // Save state before drawing line

                        if(_beforeLineSnapshot != null)
                        {
                           _editable = _beforeLineSnapshot.Clone();
                        }

                        DrawLine((int)_lineStart.Value.X, (int)_lineStart.Value.Y, x, y, _drawValue);

                        _lineStart = null;
                        _beforeLineSnapshot = null;

                        GlyphEdited?.Invoke(GlyphId, _editable);
                        RequestRedraw();

                        // Auto-switch back to Pencil after completing line
                        CurrentTool = DrawTool.Pencil;
                     }
                  }
                  else if(_currentTool == DrawTool.Rectangle)
                        {
                           if(_rectStart == null)
                           {
                              // First click - set start corner
                              _rectStart = new Point(x, y);
                              _beforeRectSnapshot = _editable.Clone();
                              _drawValue = !_editable.GetPixel(x, y);
                           }
                              else
                              {
                                 // Second click - draw final rectangle and reset
                                 SaveSnapshot();

                                 if(_beforeRectSnapshot != null)
                                 {
                                    _editable = _beforeRectSnapshot.Clone();
                                 }

                                 DrawRectangle((int)_rectStart.Value.X, (int)_rectStart.Value.Y, x, y, _drawValue);

                                 _rectStart = null;
                                 _beforeRectSnapshot = null;

                                 GlyphEdited?.Invoke(GlyphId, _editable);
                                 RequestRedraw();

                                 // Auto-switch back to Pencil after completing rectangle
                                 CurrentTool = DrawTool.Pencil;
                              }
                           }
                           else if(_currentTool == DrawTool.Circle)
                                 {
                                    if(_circleCenter == null)
                                    {
                                       // First click - set center
                                       _circleCenter = new Point(x, y);
                                       _beforeCircleSnapshot = _editable.Clone();
                                       _drawValue = !_editable.GetPixel(x, y);
                                    }
                                       else
                                       {
                                          // Second click - draw final circle and reset
                                          SaveSnapshot();

                                          if(_beforeCircleSnapshot != null)
                                          {
                                             _editable = _beforeCircleSnapshot.Clone();
                                          }

                                          int radius = (int)Math.Round(Math.Sqrt(
                                             Math.Pow(x - _circleCenter.Value.X, 2) + 
                                             Math.Pow(y - _circleCenter.Value.Y, 2)));

                                          DrawCircle((int)_circleCenter.Value.X, (int)_circleCenter.Value.Y, radius, _drawValue);

                                          _circleCenter = null;
                                          _beforeCircleSnapshot = null;

                                          GlyphEdited?.Invoke(GlyphId, _editable);
                                          RequestRedraw();

                                          // Auto-switch back to Pencil after completing circle
                                          CurrentTool = DrawTool.Pencil;
                                       }
                                    }
                                    else if(_currentTool == DrawTool.Fill)
                                          {
                                             // Fill tool - single click to flood fill
                                             SaveSnapshot();

                                             bool targetValue = _editable.GetPixel(x, y);
                                             bool fillValue = !targetValue; // Toggle: if pixel is ON, fill with OFF (erase area). If OFF, fill with ON (paint area).

                                             if(targetValue != fillValue) // Only fill if different
                                             {
                                                FloodFill(x, y, targetValue, fillValue);
                                                GlyphEdited?.Invoke(GlyphId, _editable);
                                                RequestRedraw();
                                             }
                                          }
                                       }

      private void OnPointerMoved(object? sender, PointerEventArgs e)
      {
         if(_editable == null) return;

         var point = e.GetPosition(_canvas);
         int scale = PixelScale <= 0 ? 10 : PixelScale;

         int x = (int)(point.X / scale);
         int y = (int)(point.Y / scale);

         if(x < 0 || y < 0 || x >= _editable.Size.Width || y >= _editable.Size.Height)
            return;

         if(_currentTool == DrawTool.Pencil)
         {
            if(!_isDrawing) return;

            // Continue drawing with the same value (paint or erase)
            if(_editable.GetPixel(x, y) != _drawValue)
            {
               _editable.SetPixel(x, y, _drawValue);
               GlyphEdited?.Invoke(GlyphId, _editable);
               RequestRedraw();
            }
         }
            else if(_currentTool == DrawTool.Line && _lineStart != null)
            {
               // Show preview of line while moving
               if(_beforeLineSnapshot != null)
               {
                  _editable = _beforeLineSnapshot.Clone();
                  DrawLine((int)_lineStart.Value.X, (int)_lineStart.Value.Y, x, y, _drawValue);
                  RequestRedraw();
               }
            }
               else if(_currentTool == DrawTool.Rectangle && _rectStart != null)
               {
                  // Show preview of rectangle while moving
                  if(_beforeRectSnapshot != null)
                  {
                     _editable = _beforeRectSnapshot.Clone();
                     DrawRectangle((int)_rectStart.Value.X, (int)_rectStart.Value.Y, x, y, _drawValue);
                     RequestRedraw();
                  }
               }
               else if(_currentTool == DrawTool.Circle && _circleCenter != null)
               {
                  // Show preview of circle while moving
                  if(_beforeCircleSnapshot != null)
                  {
                     _editable = _beforeCircleSnapshot.Clone();

                     int radius = (int)Math.Round(Math.Sqrt(
                        Math.Pow(x - _circleCenter.Value.X, 2) + 
                        Math.Pow(y - _circleCenter.Value.Y, 2)));

                     DrawCircle((int)_circleCenter.Value.X, (int)_circleCenter.Value.Y, radius, _drawValue);
                     RequestRedraw();
                  }
               }
            }

         private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
         {
            _isDrawing = false;
         }

         // Bresenham's line algorithm
         private void DrawLine(int x0, int y0, int x1, int y1, bool value)
         {
            if(_editable == null) return;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while(true)
            {
               if(x0 >= 0 && y0 >= 0 && x0 < _editable.Size.Width && y0 < _editable.Size.Height)
               {
                  _editable.SetPixel(x0, y0, value);
               }

               if(x0 == x1 && y0 == y1)
                  break;

               int e2 = 2 * err;
               if(e2 > -dy)
               {
                  err -= dy;
                  x0 += sx;
               }
               if(e2 < dx)
               {
                  err += dx;
                  y0 += sy; // CRITICAL FIX: Move Y coordinate!
               }
            }
         }

                     // Draw rectangle outline
                     private void DrawRectangle(int x0, int y0, int x1, int y1, bool value)
                     {
                        if(_editable == null) return;

                        // Normalize coordinates (in case user drags from bottom-right to top-left)
                        int left = Math.Min(x0, x1);
                        int right = Math.Max(x0, x1);
                        int top = Math.Min(y0, y1);
                        int bottom = Math.Max(y0, y1);

                        // Draw four edges
                        // Top edge
                        for(int x = left; x <= right; x++)
                           SetPixelSafe(x, top, value);

                        // Bottom edge
                        for(int x = left; x <= right; x++)
                           SetPixelSafe(x, bottom, value);

                        // Left edge
                        for(int y = top; y <= bottom; y++)
                           SetPixelSafe(left, y, value);

                        // Right edge
                        for(int y = top; y <= bottom; y++)
                           SetPixelSafe(right, y, value);
                     }

                        private void SetPixelSafe(int x, int y, bool value)
                        {
                           if(_editable == null) return;
                           if(x >= 0 && y >= 0 && x < _editable.Size.Width && y < _editable.Size.Height)
                              _editable.SetPixel(x, y, value);
                        }

                        // Bresenham's circle algorithm
                        private void DrawCircle(int centerX, int centerY, int radius, bool value)
                        {
                           if(_editable == null || radius < 0) return;

                           int x = 0;
                           int y = radius;
                           int d = 3 - 2 * radius;

                           DrawCirclePoints(centerX, centerY, x, y, value);

                           while(y >= x)
                           {
                              x++;

                              if(d > 0)
                              {
                                 y--;
                                 d = d + 4 * (x - y) + 10;
                              }
                              else
                              {
                                 d = d + 4 * x + 6;
                              }

                              DrawCirclePoints(centerX, centerY, x, y, value);
                           }
                        }

                           private void DrawCirclePoints(int centerX, int centerY, int x, int y, bool value)
                           {
                              // Draw all 8 octants
                              SetPixelSafe(centerX + x, centerY + y, value);
                              SetPixelSafe(centerX - x, centerY + y, value);
                              SetPixelSafe(centerX + x, centerY - y, value);
                              SetPixelSafe(centerX - x, centerY - y, value);
                              SetPixelSafe(centerX + y, centerY + x, value);
                              SetPixelSafe(centerX - y, centerY + x, value);
                              SetPixelSafe(centerX + y, centerY - x, value);
                              SetPixelSafe(centerX - y, centerY - x, value);
                           }

                           // Flood fill algorithm (stack-based to avoid recursion stack overflow)
                           private void FloodFill(int startX, int startY, bool targetValue, bool fillValue)
                           {
                              if(_editable == null) return;
                              if(startX < 0 || startY < 0 || startX >= _editable.Size.Width || startY >= _editable.Size.Height)
                                 return;

                              var stack = new Stack<(int x, int y)>();
                              stack.Push((startX, startY));

                              while(stack.Count > 0)
                              {
                                 var (x, y) = stack.Pop();

                                 if(x < 0 || y < 0 || x >= _editable.Size.Width || y >= _editable.Size.Height)
                                    continue;

                                 if(_editable.GetPixel(x, y) != targetValue)
                                    continue;

                                 _editable.SetPixel(x, y, fillValue);

                                 // Add neighboring pixels (4-way connectivity)
                                 stack.Push((x + 1, y));
                                 stack.Push((x - 1, y));
                                 stack.Push((x, y + 1));
                                 stack.Push((x, y - 1));
                              }
                           }
                        }

