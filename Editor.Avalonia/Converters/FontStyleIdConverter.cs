using Avalonia.Data.Converters;
using Storage.Abstractions.Models;
using System;
using System.Globalization;

namespace Editor.Avalonia.Converters;

public class FontStyleIdConverter : IValueConverter
{
   public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
   {
      if (value is FontStyleId styleId && !string.IsNullOrEmpty(styleId.Name))
         return styleId;
      
      return null;
   }

   public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
   {
      // When ComboBox has no selection or null value, return a default FontStyleId
      if (value == null)
         return new FontStyleId("_base_");
      
      if (value is FontStyleId styleId)
         return styleId;
      
      return new FontStyleId("_base_");
   }
}
