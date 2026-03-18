using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace FoodMapAPI.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public TranslationService(IMemoryCache cache)
        {
            _httpClient = new HttpClient();
            _cache = cache;
        }

        public async Task<string> TranslateAsync(string text, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text) || targetLang == "vi")
                return text;

            string cacheKey = $"trans_{targetLang}_{text.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out string cachedResult))
            {
                return cachedResult;
            }

            try
            {
                string fromLang = "vi";
                string toLang = targetLang.Split('-')[0]; // en-US -> en, zh-CN -> zh

                string url = $"https://api.mymemory.translated.net/get?q={WebUtility.UrlEncode(text)}&langpair={fromLang}|{toLang}";

                var response = await _httpClient.GetStringAsync(url);
                using (var doc = JsonDocument.Parse(response))
                {
                    if (doc.RootElement.TryGetProperty("responseData", out var responseData))
                    {
                        if (responseData.TryGetProperty("translatedText", out var translatedText))
                        {
                            string result = translatedText.GetString() ?? text;
                            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
                            return result;
                        }
                    }
                }
                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translation error: {ex.Message}");
                return text;
            }
        }

        public async Task<string> TranslateCachedAsync(string text, string targetLang)
        {
            return await TranslateAsync(text, targetLang);
        }
    }
}
