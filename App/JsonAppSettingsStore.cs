using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tools
{
   public sealed class JsonAppSettingsStore : IAppSettingsStore
   {
      private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
      {
         WriteIndented = true
      };

      public async Task<AppSettings> LoadAsync()
      {
         AppPaths.EnsureFolders();
         var path = AppPaths.UserSettingsFile();

         if(!File.Exists(path))
         {
            var fresh = new AppSettings();
            fresh.EnsureDefaults();
            await SaveAsync(fresh).ConfigureAwait(false);
            return fresh;
         }

         var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
         var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
         settings.EnsureDefaults();
         return settings;
      }

      public async Task SaveAsync(AppSettings settings)
      {
         if(settings == null) throw new ArgumentNullException(nameof(settings));

         AppPaths.EnsureFolders();
         settings.EnsureDefaults();

         var path = AppPaths.UserSettingsFile();
         var json = JsonSerializer.Serialize(settings, JsonOptions);
         await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
      }
   }
}
