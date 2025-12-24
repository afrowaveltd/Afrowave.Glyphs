using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Storage.FileSystem;

namespace Tools
{
   public sealed class SettingsService
   {
      private readonly IAppSettingsStore _store;

      public AppSettings Current { get; private set; } = new AppSettings();

      public SettingsService(IAppSettingsStore store)
      {
         _store = store ?? throw new ArgumentNullException(nameof(store));
      }

      public async Task InitializeAsync()
      {
         Current = await _store.LoadAsync().ConfigureAwait(false);
         EnsureWorkspaceRootExists(Current.GetActiveSymbolsRoot());
      }

      public async Task AddSymbolsRootAsync(string path)
      {
         if(string.IsNullOrWhiteSpace(path)) return;

         path = path.Trim();
         EnsureWorkspaceRootExists(path);

         if(!Current.SymbolsRoots.Contains(path, StringComparer.OrdinalIgnoreCase))
            Current.SymbolsRoots.Add(path);

         await _store.SaveAsync(Current).ConfigureAwait(false);
      }

      public async Task SetActiveRootAsync(int index)
      {
         Current.ActiveSymbolsRootIndex = index;
         Current.EnsureDefaults();
         EnsureWorkspaceRootExists(Current.GetActiveSymbolsRoot());
         await _store.SaveAsync(Current).ConfigureAwait(false);
      }

      public Task SaveAsync() => _store.SaveAsync(Current);

      private static void EnsureWorkspaceRootExists(string root)
      {
         Directory.CreateDirectory(root);
         _ = SymbolsFolderResolver.ResolveSymbolsRoot(root, createIfMissing: true);
      }
   }
}
