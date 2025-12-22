using System.Threading.Tasks;

namespace Tools
{
   public interface IAppSettingsStore
   {
      Task<AppSettings> LoadAsync();
      Task SaveAsync(AppSettings settings);
   }
}
