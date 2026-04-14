using System.Text.Json;
using Microsoft.Maui.Storage;

namespace FoodMapApp.Services
{
    public static class CacheService
    {
        private static string CachePath => FileSystem.CacheDirectory;

        public static async Task SaveCacheAsync(string key, string data)
        {
            try
            {
                string filePath = Path.Combine(CachePath, $"{key}.json");
                await File.WriteAllTextAsync(filePath, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CacheService Save Error: {ex.Message}");
            }
        }

        public static async Task<string?> GetCacheAsync(string key)
        {
            try
            {
                string filePath = Path.Combine(CachePath, $"{key}.json");
                if (File.Exists(filePath))
                {
                    return await File.ReadAllTextAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CacheService Get Error: {ex.Message}");
            }
            return null;
        }

        public static async Task<T?> GetCacheAsync<T>(string key)
        {
            string? json = await GetCacheAsync(key);
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

        public static void ClearCache()
        {
            try
            {
                var files = Directory.GetFiles(CachePath, "*.json");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch { }
        }
    }
}
