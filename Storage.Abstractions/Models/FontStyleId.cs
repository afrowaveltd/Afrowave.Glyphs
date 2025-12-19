using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.Abstractions.Models
{
   public readonly struct FontStyleId : IEquatable<FontStyleId>
   {
      public string Name { get; }

      public FontStyleId(string name)
      {
         name = (name ?? string.Empty).Trim();
         if(name.Length == 0) throw new ArgumentException("Style name cannot be empty.", nameof(name));
         Name = name;
      }

      public override string ToString() => Name;

      public bool Equals(FontStyleId other) => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

      public override bool Equals(object obj) => obj is FontStyleId other && Equals(other);

      public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

      public static bool operator ==(FontStyleId left, FontStyleId right) => left.Equals(right);

      public static bool operator !=(FontStyleId left, FontStyleId right) => !left.Equals(right);
   }
}