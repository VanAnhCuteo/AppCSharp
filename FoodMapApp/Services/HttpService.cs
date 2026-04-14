using System.Net.Http.Json;
using System.Text.Json;

namespace FoodMapApp.Services
{
    public static class HttpService
    {
        private static readonly HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static HttpClient Client => _client;

        public static async Task<T?> GetAsync<T>(string url)
        {
            try
            {
                var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"HttpService GET {url} failed: {(int)response.StatusCode}");
                    return default;
                }
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpService GET {url} error: {ex.Message}");
                return default;
            }
        }

        public static async Task<T?> GetWithCacheAsync<T>(string url, string cacheKey)
        {
            // 1. Try Network
            try
            {
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // Save to cache in background
                    _ = CacheService.SaveCacheAsync(cacheKey, json);
                    return JsonSerializer.Deserialize<T>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpService GetWithCache network failed: {ex.Message}");
            }

            // 2. Fallback to Cache
            return await CacheService.GetCacheAsync<T>(cacheKey);
        }

        public static async Task<string?> GetStringAsync(string url)
        {
            try
            {
                var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpService GetString {url} error: {ex.Message}");
                return null;
            }
        }

        public static async Task<string?> GetStringWithCacheAsync(string url, string cacheKey)
        {
            // 1. Try Network
            try
            {
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    _ = CacheService.SaveCacheAsync(cacheKey, data);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpService GetStringWithCache network failed: {ex.Message}");
            }

            // 2. Fallback to Cache
            return await CacheService.GetCacheAsync(cacheKey);
        }
    }
}
