using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using FoodMapApp.Models;

namespace FoodMapApp.Services
{
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private Dictionary<string, Dictionary<string, string>> _languageCache = new();
        private Dictionary<string, string> _cache = new();
        private string _currentLang = "vi";
        public List<LanguageModel> AvailableLanguages { get; private set; } = new();
        private const string CacheFileName = "ui_translations_cache_v2.json";

        private LocalizationService()
        {
            _currentLang = Preferences.Default.Get("app_lang", "vi");
            LoadFromDisk();
        }

        public string CurrentLanguage
        {
            get => _currentLang;
            set
            {
                if (_currentLang != value)
                {
                    _currentLang = value;
                    Preferences.Default.Set("app_lang", value);
                    if (_languageCache.TryGetValue(value, out var cached))
                    {
                         _cache = cached;
                    }
                }
            }
        }

        public string Get(string key, string? defaultValue = null)
        {
            if (_cache.TryGetValue(key, out var val)) return val;
            return defaultValue ?? key;
        }

        public string GetLanguageName(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Tiếng Việt";
            var lang = AvailableLanguages.FirstOrDefault(l => l.language_code == code);
            return lang?.name ?? (code == "vi" ? "Tiếng Việt" : code);
        }

        public async Task RefreshLanguagesAsync()
        {
            try
            {
                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var list = await client.GetFromJsonAsync<List<LanguageModel>>($"{AppConfig.LanguageApiUrl}");
                if (list != null && list.Any())
                {
                    AvailableLanguages = list;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching languages: {ex.Message}");
                if (!AvailableLanguages.Any())
                {
                    AvailableLanguages = new List<LanguageModel>
                    {
                        new LanguageModel { language_code = "vi", name = "Tiếng Việt" },
                        new LanguageModel { language_code = "en", name = "English" }
                    };
                }
            }
        }

        public async Task InitializeAsync(string lang, Dictionary<string, string> sourceStrings)
        {
            CurrentLanguage = lang;
            
            if (!_languageCache.ContainsKey(lang))
            {
                _languageCache[lang] = new Dictionary<string, string>();
                _cache = _languageCache[lang];
            }
            else
            {
                _cache = _languageCache[lang];
            }

            if (lang == "vi")
            {
                foreach(var kv in sourceStrings) _cache[kv.Key] = kv.Value;
                SaveToDisk();
                return;
            }

            var missingStrings = sourceStrings.Where(kv => !_cache.ContainsKey(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

            if (!missingStrings.Any())
            {
                return; // Nothing to translate
            }

            try
            {
                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var payload = new { TargetLang = lang.Split('-')[0], Strings = missingStrings };
                var response = await client.PostAsJsonAsync($"{AppConfig.FoodApiUrl}/translate-ui", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (result != null)
                    {
                        foreach(var kv in result)
                        {
                            _cache[kv.Key] = kv.Value;
                        }
                        SaveToDisk();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Localization fetch error: {ex.Message}");
            }
        }

        private void SaveToDisk()
        {
            try
            {
                string path = Path.Combine(FileSystem.CacheDirectory, CacheFileName);
                string json = JsonSerializer.Serialize(_languageCache);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private void LoadFromDisk()
        {
            try
            {
                string path = Path.Combine(FileSystem.CacheDirectory, CacheFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                    if (data != null) 
                    {
                        _languageCache = data;
                        if (_languageCache.TryGetValue(_currentLang, out var cached))
                        {
                            _cache = cached;
                        }
                    }
                }
            }
            catch { }
        }
    }
}
