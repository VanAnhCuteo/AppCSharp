using System.Net.Http.Json;

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
    }
}
