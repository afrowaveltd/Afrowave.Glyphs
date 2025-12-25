using System;
using System.IO;

namespace Tools
{
   public static class AppPaths
   {
      private const string VendorFolder = "Afrowave";
      private const string AppFolder = "GlyphEditor";

      public static string UserHomeAfrowave()
      {
         // Cross-platform: user profile
         // Windows: C:\Users\<u>\  | Linux: /home/<u> | macOS: /Users/<u>
         var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
         return Path.Combine(home, VendorFolder);
      }

      public static string UserDocumentsAfrowave()
      {
         var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
         if(string.IsNullOrWhiteSpace(docs))
            docs = UserHomeAfrowave(); // fallback
         return Path.Combine(docs, VendorFolder);
      }

      public static string DefaultSymbolsRoot()
      {
         // Friendly & visible location
         return Path.Combine(UserDocumentsAfrowave(), AppFolder, "Symbols");
      }

      public static string UserSettingsFolder()
      {
         // Visible but not messy: ~/.Afrowave/GlyphEditor/Settings
         // (You can also use AppData on Windows, but your Afrowave rule prefers a dedicated Afrowave folder)
         return Path.Combine(UserHomeAfrowave(), AppFolder, "Settings");
      }

      public static string UserSettingsFile()
      {
         return Path.Combine(UserSettingsFolder(), "settings.json");
      }

      public static string TempFolder()
      {
         return Path.Combine(Path.GetTempPath(), VendorFolder, AppFolder);
      }

      public static string GetSystemFontsDirectory()
      {
         // Try user-installed fonts first (Windows 11+)
         var userFonts = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Fonts"
         );

         if(Directory.Exists(userFonts))
            return userFonts;

         // Fallback to system fonts
         var systemFonts = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Fonts"
         );

         if(Directory.Exists(systemFonts))
            return systemFonts;

         // Last fallback - Documents
         return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      }

      public static void EnsureFolders()
      {
         Directory.CreateDirectory(UserHomeAfrowave());
         Directory.CreateDirectory(UserSettingsFolder());
         Directory.CreateDirectory(DefaultSymbolsRoot());
         Directory.CreateDirectory(TempFolder());
      }
   }
}
