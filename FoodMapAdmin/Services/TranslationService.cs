using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace FoodMapAdmin.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string targetLang);
    }

    public class TranslationService : ITranslationService
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
                // Map to Google Translate compatible codes
                string toLang = targetLang switch
                {
                    "zh" => "zh-CN",
                    _ => targetLang
                };

                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={toLang}&dt=t&q={WebUtility.UrlEncode(text)}";

                var response = await _httpClient.GetStringAsync(url);
                using (var doc = JsonDocument.Parse(response))
                {
                    // Response format: [[["Hello","Xin chào",null,null,1]],null,"vi"]
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var segments = doc.RootElement[0];
                        if (segments.ValueKind == JsonValueKind.Array)
                        {
                            string translatedText = "";
                            foreach (var segment in segments.EnumerateArray())
                            {
                                if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0)
                                {
                                    translatedText += segment[0].GetString() ?? "";
                                }
                            }
                            
                            if (!string.IsNullOrWhiteSpace(translatedText))
                            {
                                _cache.Set(cacheKey, translatedText, TimeSpan.FromHours(24));
                                return translatedText;
                            }
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
    }
}
