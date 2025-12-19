using Core.Models;
using Core.Text;
using Storage.Abstractions.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime
{
   public sealed class TextToTerminalRenderer
   {
      private readonly GlyphResolver _resolver;

      public TextToTerminalRenderer(GlyphResolver resolver)
      {
         _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
      }

      public async Task<TerminalBuffer> RenderAsync(
          string packId,
          FontStyleId activeStyle,
          string text,
          int terminalWidth,
          int terminalHeight,
          GlyphId fallbackGlyph,
          TextLayoutOptions options,
          CancellationToken ct)
      {
         if(terminalWidth <= 0) throw new ArgumentOutOfRangeException(nameof(terminalWidth));
         if(terminalHeight <= 0) throw new ArgumentOutOfRangeException(nameof(terminalHeight));

         options ??= new TextLayoutOptions();
         text ??= string.Empty;

         var buffer = new TerminalBuffer(terminalWidth, terminalHeight);

         int x = 0;
         int y = 0;

         bool lastWasCR = false;

         // Enumerate Unicode scalar values (code points), so surrogate pairs are handled correctly.
         foreach(int codePoint in UnicodeCodePointEnumerator.Enumerate(text))
         {
            ct.ThrowIfCancellationRequested();

            // CRLF handling:
            // - '\r' always triggers newline and sets lastWasCR=true
            // - '\n' triggers newline only if previous was not '\r'
            if(codePoint == '\r')
            {
               x = 0;
               y++;
               lastWasCR = true;

               if(y >= terminalHeight) break;
               continue;
            }

            if(codePoint == '\n')
            {
               if(lastWasCR)
               {
                  // This is the LF of CRLF -> ignore (already moved line on CR)
                  lastWasCR = false;
                  continue;
               }

               x = 0;
               y++;
               lastWasCR = false;

               if(y >= terminalHeight) break;
               continue;
            }

            // Any non-newline character clears the CRLF state.
            lastWasCR = false;

            if(codePoint == '\t')
            {
               int tabWidth = options.TabWidth <= 0 ? 4 : options.TabWidth;
               int nextStop = ((x / tabWidth) + 1) * tabWidth;
               int spaces = nextStop - x;

               for(int s = 0; s < spaces; s++)
               {
                  if(y >= terminalHeight) break;

                  // Render as space
                  (x, y) = await PutCodePointAsync(packId, activeStyle, buffer, x, y, (int)' ', fallbackGlyph, options, terminalWidth, terminalHeight, ct)
                      .ConfigureAwait(false);
               }

               continue;
            }

            (x, y) = await PutCodePointAsync(packId, activeStyle, buffer, x, y, codePoint, fallbackGlyph, options, terminalWidth, terminalHeight, ct)
                .ConfigureAwait(false);

            if(y >= terminalHeight) break;
         }

         return buffer;
      }

      private async Task<(int x, int y)> PutCodePointAsync(
          string packId,
          FontStyleId activeStyle,
          TerminalBuffer buffer,
          int x,
          int y,
          int codePoint,
          GlyphId fallbackGlyph,
          TextLayoutOptions options,
          int terminalWidth,
          int terminalHeight,
          CancellationToken ct)
      {
         if(y >= terminalHeight) return (x, y);

         // Wrap / Clip behavior
         if(x >= terminalWidth)
         {
            if(options.Wrap)
            {
               x = 0;
               y++;
               if(y >= terminalHeight) return (x, y);
            }
            else
            {
               if(options.Clip) return (x, y);
               x = terminalWidth - 1; // last cell overwrite if not clipping
            }
         }

         GlyphId requested = GlyphId.FromUnicode(codePoint);

         var resolved = await _resolver.ResolveAsync(packId, activeStyle, requested, fallbackGlyph, ct)
             .ConfigureAwait(false);

         bool isFallback = resolved.IsFallback || !resolved.ResolvedId.Equals(requested);

         buffer.SetCell(x, y, new TerminalCell(requested, resolved.ResolvedId, isFallback));

         x++;
         return (x, y);
      }
   }
}