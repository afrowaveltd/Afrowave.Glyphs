using Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace Tools.FontImport
{
   public sealed class FontGlyphRasterizer
   {
      private readonly byte _threshold;

      public FontGlyphRasterizer(byte threshold = 128)
      {
         _threshold = threshold;
      }

      public Task<GlyphBitmap> RasterizeAsync(string fontPath, int codePoint, GridSize size, CancellationToken ct)
      {
         if(string.IsNullOrWhiteSpace(fontPath)) throw new ArgumentException("fontPath is null or empty.", nameof(fontPath));
         if(!File.Exists(fontPath)) throw new FileNotFoundException("Font file not found.", fontPath);
         if(codePoint < 0 || codePoint > 0x10FFFF) throw new ArgumentOutOfRangeException(nameof(codePoint));

         // no async IO here, keep signature async-friendly
         ct.ThrowIfCancellationRequested();

         using var typeface = SKTypeface.FromFile(fontPath);
         if(typeface == null) throw new InvalidOperationException("Failed to load font: " + fontPath);

         using var paint = new SKPaint
         {
            Typeface = typeface,
            IsAntialias = true,
            LcdRenderText = false,
            SubpixelText = false,
            HintingLevel = SKPaintHinting.Normal,
            Color = SKColors.White
         };

         // Fit glyph into bitmap. Use a generous font size and then scale-to-fit.
         float targetW = size.Width;
         float targetH = size.Height;

         // Start with a size that usually exceeds the target; then scale down.
         float initialSize = Math.Max(targetW, targetH) * 2.0f;
         paint.TextSize = initialSize;

         var s = char.ConvertFromUtf32(codePoint);

         // Measure at initial size, then scale down to fit.
         var initialBounds = new SKRect();
         float initialW = paint.MeasureText(s, ref initialBounds);
         var fm = paint.FontMetrics;
         float initialH = fm.Descent - fm.Ascent;

         if(initialW <= 0 || initialH <= 0)
            return Task.FromResult(new GlyphBitmap(size, new byte[GlyphBitmap.GetByteLength(size)]));

         float scaleX = targetW / initialW;
         float scaleY = targetH / initialH;
         float scale = Math.Min(scaleX, scaleY);
         float finalSize = Math.Max(1f, initialSize * scale);
         paint.TextSize = finalSize;

         var bounds = new SKRect();
         float textW = paint.MeasureText(s, ref bounds);
         fm = paint.FontMetrics;
         float textH = fm.Descent - fm.Ascent;

         // Prepare raster surface
         var info = new SKImageInfo(size.Width, size.Height, SKColorType.Alpha8, SKAlphaType.Premul);
         using var surface = SKSurface.Create(info);
         if(surface == null) throw new InvalidOperationException("Failed to create SKSurface.");

         var canvas = surface.Canvas;
         canvas.Clear(SKColors.Transparent);

         // Center glyph in the cell
         // Baseline positioning using font metrics (ascent is negative).
         float x = (targetW - textW) / 2f - bounds.Left;
         float y = (targetH - textH) / 2f - fm.Ascent;

         canvas.DrawText(s, x, y, paint);
         canvas.Flush();

         using var snapshot = surface.Snapshot();
         using var pix = snapshot.PeekPixels();

         var bmp = new GlyphBitmap(size, new byte[GlyphBitmap.GetByteLength(size)]);

         for(int yy = 0; yy < size.Height; yy++)
         {
            ct.ThrowIfCancellationRequested();

            for(int xx = 0; xx < size.Width; xx++)
            {
               var c = pix.GetPixelColor(xx, yy);
               bool on = c.Alpha >= _threshold;
               if(on)
                  bmp.SetPixel(xx, yy, true);
            }
         }

         return Task.FromResult(bmp);
      }
   }
}
