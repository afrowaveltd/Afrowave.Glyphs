using Core.Models;
using Storage.Abstractions.Abstractions;
using Storage.Abstractions.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tools
{
    public static class SampleFontGenerator
    {
        public static async Task CreateSampleFontAsync(IGlyphRepository repo, string packId, GridSize size, CancellationToken ct)
        {
            var style = new FontStyleId("_base_");
            
            // Create basic ASCII characters (32-126)
            for (int codePoint = 32; codePoint <= 126; codePoint++)
            {
                var id = GlyphId.FromUnicode(codePoint);
                var bitmap = GenerateCharacterBitmap((char)codePoint, size);
                var glyph = new Glyph(id, size, bitmap);
                
                await repo.SaveAsync(packId, style, glyph, ct);
            }
        }

        private static GlyphBitmap GenerateCharacterBitmap(char ch, GridSize size)
        {
            // Simple 5x7 font patterns embedded here for basic ASCII
            // This is a minimal implementation - you can expand with better patterns
            var data = new byte[GlyphBitmap.GetByteLength(size)];
            var bitmap = new GlyphBitmap(size, data);

            // For now, create a simple representation
            // You can replace this with actual font rasterization
            switch (ch)
            {
                case 'H':
                    if (size.Width >= 5 && size.Height >= 7)
                    {
                        // H pattern
                        SetPixel(bitmap, 0, 0); SetPixel(bitmap, 0, 1); SetPixel(bitmap, 0, 2); SetPixel(bitmap, 0, 3); SetPixel(bitmap, 0, 4); SetPixel(bitmap, 0, 5); SetPixel(bitmap, 0, 6);
                        SetPixel(bitmap, 2, 3);
                        SetPixel(bitmap, 4, 0); SetPixel(bitmap, 4, 1); SetPixel(bitmap, 4, 2); SetPixel(bitmap, 4, 3); SetPixel(bitmap, 4, 4); SetPixel(bitmap, 4, 5); SetPixel(bitmap, 4, 6);
                    }
                    break;
                case 'e':
                    if (size.Width >= 5 && size.Height >= 7)
                    {
                        SetPixel(bitmap, 1, 3); SetPixel(bitmap, 2, 3); SetPixel(bitmap, 3, 3);
                        SetPixel(bitmap, 0, 4); SetPixel(bitmap, 4, 4);
                        SetPixel(bitmap, 0, 5); SetPixel(bitmap, 1, 5); SetPixel(bitmap, 2, 5); SetPixel(bitmap, 3, 5); SetPixel(bitmap, 4, 5);
                        SetPixel(bitmap, 0, 6);
                        SetPixel(bitmap, 1, 7); SetPixel(bitmap, 2, 7); SetPixel(bitmap, 3, 7);
                    }
                    break;
                case 'l':
                    if (size.Width >= 5 && size.Height >= 7)
                    {
                        SetPixel(bitmap, 2, 0); SetPixel(bitmap, 2, 1); SetPixel(bitmap, 2, 2); SetPixel(bitmap, 2, 3); SetPixel(bitmap, 2, 4); SetPixel(bitmap, 2, 5); SetPixel(bitmap, 2, 6);
                    }
                    break;
                default:
                    // For other characters, create a simple placeholder pattern
                    if (ch >= 33 && ch <= 126 && size.Width >= 5 && size.Height >= 7)
                    {
                        // Simple box pattern for visibility
                        for (int y = 1; y < size.Height - 1; y++)
                        {
                            SetPixel(bitmap, 1, y);
                            if (y == size.Height / 2)
                            {
                                SetPixel(bitmap, 2, y);
                                SetPixel(bitmap, 3, y);
                            }
                        }
                    }
                    break;
            }

            return bitmap;
        }

        private static void SetPixel(GlyphBitmap bitmap, int x, int y)
        {
            if (x >= 0 && x < bitmap.Size.Width && y >= 0 && y < bitmap.Size.Height)
            {
                bitmap.SetPixel(x, y, true);
            }
        }
    }
}
