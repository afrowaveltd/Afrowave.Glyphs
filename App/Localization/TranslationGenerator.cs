using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tools.Localization
{
    /// <summary>
    /// Helper for generating translations using LibreTranslate API
    /// </summary>
    public class TranslationGenerator
    {
        private readonly string _libreTranslateUrl;
        private readonly HttpClient _httpClient;

        public TranslationGenerator(string libreTranslateUrl = "https://libretranslate.com")
        {
            _libreTranslateUrl = libreTranslateUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Generate translations for all supported languages
        /// </summary>
        public async Task GenerateTranslationsAsync(
            string sourceLanguage,
            string localesDirectory,
            params string[] targetLanguages)
        {
            // Load source dictionary (e.g., en.json)
            var sourcePath = Path.Combine(localesDirectory, $"{sourceLanguage}.json");
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Source language file not found: {sourcePath}");
            }

            var sourceJson = await File.ReadAllTextAsync(sourcePath);
            var sourceDict = JsonSerializer.Deserialize<Dictionary<string, string>>(sourceJson);
            
            if (sourceDict == null)
                return;

            foreach (var targetLang in targetLanguages)
            {
                Console.WriteLine($"Generating translations for: {targetLang}");
                
                var targetDict = new Dictionary<string, string>();

                foreach (var (key, value) in sourceDict)
                {
                    try
                    {
                        var translated = await TranslateAsync(value, sourceLanguage, targetLang);
                        targetDict[key] = translated;
                        
                        Console.WriteLine($"  {key}: {value} -> {translated}");
                        
                        // Small delay to avoid rate limiting
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error translating {key}: {ex.Message}");
                        targetDict[key] = value; // Fallback to original
                    }
                }

                // Save target language file
                var targetPath = Path.Combine(localesDirectory, $"{targetLang}.json");
                var targetJson = JsonSerializer.Serialize(targetDict, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                await File.WriteAllTextAsync(targetPath, targetJson);
                Console.WriteLine($"Saved: {targetPath}");
            }
        }

        /// <summary>
        /// Translate a single text using LibreTranslate
        /// </summary>
        public async Task<string> TranslateAsync(
            string text, 
            string sourceLanguage, 
            string targetLanguage)
        {
            var url = $"{_libreTranslateUrl}/translate";
            
            var payload = new
            {
                q = text,
                source = sourceLanguage,
                target = targetLanguage,
                format = "text"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TranslateResponse>(responseJson);

            return result?.TranslatedText ?? text;
        }

        private class TranslateResponse
        {
            public string TranslatedText { get; set; } = "";
        }
    }

    /// <summary>
    /// Common supported languages
    /// </summary>
    public static class SupportedLanguages
    {
        public const string English = "en";
        public const string Czech = "cs";
        public const string German = "de";
        public const string Spanish = "es";
        public const string French = "fr";
        public const string Italian = "it";
        public const string Polish = "pl";
        public const string Russian = "ru";
        public const string Japanese = "ja";
        public const string Chinese = "zh";
        public const string Korean = "ko";
        public const string Portuguese = "pt";
        public const string Dutch = "nl";
        public const string Swedish = "sv";
        public const string Turkish = "tr";
        public const string Arabic = "ar";
        public const string Hindi = "hi";

        public static string[] All => new[]
        {
            Czech, German, Spanish, French, Italian, Polish, Russian,
            Japanese, Chinese, Korean, Portuguese, Dutch, Swedish,
            Turkish, Arabic, Hindi
        };
    }
}

// Příklad použití:
// 
// var generator = new TranslationGenerator();
// await generator.GenerateTranslationsAsync(
//     "en",  // source
//     "Locales",  // directory
//     SupportedLanguages.All  // target languages
// );
