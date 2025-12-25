using Core.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tools
{

public sealed class GlyphEditorViewModel : INotifyPropertyChanged
{
   private GlyphBitmap _bitmap;

   public event PropertyChangedEventHandler? PropertyChanged;

   public GlyphBitmap Bitmap
   {
      get => _bitmap;
      set
      {
         if(value == null) throw new ArgumentNullException(nameof(value));
         _bitmap = value;
         ClampCursor();
         RebuildText();
      }
   }

   private int _cursorX;
   public int CursorX { get => _cursorX; private set { if(Set(ref _cursorX, value)) RebuildText(); } }

   private int _cursorY;
   public int CursorY { get => _cursorY; private set { if(Set(ref _cursorY, value)) RebuildText(); } }

   private string _gridText = string.Empty;
   public string GridText { get => _gridText; private set => Set(ref _gridText, value); }

   public string HelpText => "Arrows/WASD move | Space toggle | C clear | I invert | F fill | Enter accept | Esc cancel";

   public GlyphEditorViewModel(GlyphBitmap bitmap)
   {
      _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
      _cursorX = 0;
      _cursorY = 0;
      RebuildText();
   }

   public void Move(int dx, int dy)
   {
      int x = CursorX + dx;
      int y = CursorY + dy;

      if(x < 0) x = 0;
      if(y < 0) y = 0;
      if(x >= Bitmap.Size.Width) x = Bitmap.Size.Width - 1;
      if(y >= Bitmap.Size.Height) y = Bitmap.Size.Height - 1;

      CursorX = x;
      CursorY = y;
   }

   public void Toggle()
   {
      bool on = Bitmap.GetPixel(CursorX, CursorY);
      Bitmap.SetPixel(CursorX, CursorY, !on);
      RebuildText();
   }

   public void Clear()
   {
      for(int y = 0; y < Bitmap.Size.Height; y++)
         for(int x = 0; x < Bitmap.Size.Width; x++)
            Bitmap.SetPixel(x, y, false);

      RebuildText();
   }

   public void Fill()
   {
      for(int y = 0; y < Bitmap.Size.Height; y++)
         for(int x = 0; x < Bitmap.Size.Width; x++)
            Bitmap.SetPixel(x, y, true);

      RebuildText();
   }

   public void Invert()
   {
      for(int y = 0; y < Bitmap.Size.Height; y++)
         for(int x = 0; x < Bitmap.Size.Width; x++)
            Bitmap.SetPixel(x, y, !Bitmap.GetPixel(x, y));

      RebuildText();
   }

   private void ClampCursor()
   {
      if(_cursorX < 0) _cursorX = 0;
      if(_cursorY < 0) _cursorY = 0;
      if(_cursorX >= Bitmap.Size.Width) _cursorX = Math.Max(0, Bitmap.Size.Width - 1);
      if(_cursorY >= Bitmap.Size.Height) _cursorY = Math.Max(0, Bitmap.Size.Height - 1);
   }

   private void RebuildText()
   {
      var sb = new StringBuilder();
      sb.AppendLine($"{Bitmap.Size.Width}x{Bitmap.Size.Height}");

      for(int y = 0; y < Bitmap.Size.Height; y++)
      {
         for(int x = 0; x < Bitmap.Size.Width; x++)
         {
            bool on = Bitmap.GetPixel(x, y);
            bool cur = x == CursorX && y == CursorY;

            if(cur)
            {
               sb.Append('[');
               sb.Append(on ? '#' : '.');
               sb.Append(']');
            }
            else
            {
               sb.Append(' ');
               sb.Append(on ? '#' : '.');
               sb.Append(' ');
            }
         }

         sb.AppendLine();
      }

      GridText = sb.ToString();
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HelpText)));
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
