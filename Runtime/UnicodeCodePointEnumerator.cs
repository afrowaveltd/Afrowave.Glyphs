using System;
using System.Collections.Generic;
using System.Text;

namespace Runtime
{
   /// <summary>
   /// Enumerates Unicode scalar values (code points) from a .NET string.
   /// Correctly handles surrogate pairs.
   /// </summary>
   public static class UnicodeCodePointEnumerator
   {
      public static IEnumerable<int> Enumerate(string text)
      {
         if(string.IsNullOrEmpty(text))
            yield break;

         for(int i = 0; i < text.Length; i++)
         {
            char ch = text[i];

            // High surrogate
            if(char.IsHighSurrogate(ch))
            {
               if(i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
               {
                  yield return char.ConvertToUtf32(ch, text[i + 1]);
                  i++; // consume low surrogate
                  continue;
               }

               // Invalid surrogate pair → replacement character
               yield return 0xFFFD;
               continue;
            }

            // Lone low surrogate → replacement
            if(char.IsLowSurrogate(ch))
            {
               yield return 0xFFFD;
               continue;
            }

            yield return ch;
         }
      }
   }
}