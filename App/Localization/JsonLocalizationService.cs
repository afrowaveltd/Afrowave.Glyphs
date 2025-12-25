using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace Tools.Localization
{
    /// <summary>
    /// Service for application localization using JSON files
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Get localized string by key
        /// </summary>
        string this[string key] { get; }

        /// <summary>
        /// Get localized string with fallback
        /// </summary>
        string Get(string key, string? fallback = null);

        /// <summary>
        /// Current language code (e.g., "en", "cs", "en-US")
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// Available languages
        /// </summary>
        IReadOnlyList<string> AvailableLanguages { get; }

        /// <summary>
        /// Set current language
        /// </summary>
        void SetLanguage(string languageCode);
    }

    public class JsonLocalizationService : ILocalizationService
    {
        private readonly string _localesDirectory;
        private Dictionary<string, string> _currentDictionary;
        private Dictionary<string, string> _fallbackDictionary;
        private string _currentLanguage;
        private List<string> _availableLanguages;

        public JsonLocalizationService(string? localesDirectory = null)
        {
            // Default: "Locales" folder next to the executable
            _localesDirectory = localesDirectory ?? Path.Combine(System.AppContext.BaseDirectory, "Locales");
            _currentDictionary = new Dictionary<string, string>();
            _fallbackDictionary = new Dictionary<string, string>();
            _currentLanguage = "en";
            _availableLanguages = new List<string>();

            Initialize();
        }

        public string this[string key] => Get(key);

        public string Get(string key, string? fallback = null)
        {
            // Try current language
            if (_currentDictionary.TryGetValue(key, out var value))
                return value;

            // Try fallback language (en.json)
            if (_fallbackDictionary.TryGetValue(key, out value))
                return value;

            // Return fallback or key itself
            return fallback ?? key;
        }

        public string CurrentLanguage => _currentLanguage;

        public IReadOnlyList<string> AvailableLanguages => _availableLanguages.AsReadOnly();

        public void SetLanguage(string languageCode)
        {
            if (!_availableLanguages.Contains(languageCode))
            {
                // Try to find partial match (e.g., "en-US" -> "en")
                var partialMatch = _availableLanguages.FirstOrDefault(l => l.StartsWith(languageCode.Split('-')[0]));
                if (partialMatch != null)
                    languageCode = partialMatch;
                else
                    return; // Language not available
            }

            _currentLanguage = languageCode;
            LoadLanguage(languageCode);
        }

        private void Initialize()
        {
            // Create locales directory if it doesn't exist
            if (!Directory.Exists(_localesDirectory))
            {
                Directory.CreateDirectory(_localesDirectory);
                
                // Create default en.json
                var defaultDict = new Dictionary<string, string>
                {
                    { "app.title", "Afrowave Glyph Editor" },
                    { "menu.file", "File" },
                    { "menu.workspace", "Workspace" },
                    { "menu.symbols", "Symbols" },
                    { "menu.import", "Import Font" },
                    { "menu.unicode", "Unicode Range" },
                    { "button.save", "Save" },
                    { "button.revert", "Revert" },
                    { "button.clear", "Clear" },
                    { "button.clone", "Clone" },
                    { "dialog.no_glyphs.title", "No Glyphs Found" },
                    { "dialog.no_glyphs.message", "The workspace doesn't contain any glyph packs. Would you like to create a sample font pack?" },
                    { "button.create_sample", "Create Sample" },
                    { "button.cancel", "Cancel" }
                };

                SaveLanguageFile("en", defaultDict);
            }

            // Scan available languages
            ScanAvailableLanguages();

            // Load fallback (en.json)
            LoadFallbackLanguage();

            // Load current language (or fallback to en)
            LoadLanguage(_currentLanguage);
        }

        private void ScanAvailableLanguages()
        {
            _availableLanguages.Clear();

            if (!Directory.Exists(_localesDirectory))
                return;

            foreach (var file in Directory.GetFiles(_localesDirectory, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                _availableLanguages.Add(fileName);
            }

            // Ensure "en" is always available
            if (!_availableLanguages.Contains("en"))
                _availableLanguages.Add("en");
        }

        private void LoadFallbackLanguage()
        {
            var fallbackPath = Path.Combine(_localesDirectory, "en.json");
            if (File.Exists(fallbackPath))
            {
                var json = File.ReadAllText(fallbackPath);
                _fallbackDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                    ?? new Dictionary<string, string>();
            }
        }

        private void LoadLanguage(string languageCode)
        {
            var langPath = Path.Combine(_localesDirectory, $"{languageCode}.json");
            
            if (!File.Exists(langPath))
            {
                // Fallback to en
                _currentDictionary = new Dictionary<string, string>(_fallbackDictionary);
                return;
            }

            var json = File.ReadAllText(langPath);
            _currentDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                ?? new Dictionary<string, string>();
        }

        private void SaveLanguageFile(string languageCode, Dictionary<string, string> dictionary)
        {
            var path = Path.Combine(_localesDirectory, $"{languageCode}.json");
            var json = JsonSerializer.Serialize(dictionary, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(path, json);
        }
    }
}
