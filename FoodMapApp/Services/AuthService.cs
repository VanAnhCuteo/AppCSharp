using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace FoodMapApp.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private static string BaseUrl => AppConfig.AuthApiUrl; 

        public AuthService()
        {
            _httpClient = new HttpClient();
        }

        public void LoginOffline()
        {
            Preferences.Default.Set("user_id", -1);
            Preferences.Default.Set("username", "Khách");
            Preferences.Default.Set("email", "offline@foodmap.com");
            Preferences.Default.Set("role", "offline");
            Preferences.Default.Set("is_logged_in", true);
        }

        public void LoginAsGuest(int id)
        {
            Preferences.Default.Set("user_id", -id);
            Preferences.Default.Set("username", $"Khách {id}");
            Preferences.Default.Set("role", "guest");
            Preferences.Default.Set("is_logged_in", true);
        }

        public async Task LogoutAsync()
        {
            try
            {
                int userId = UserId;
                if (userId != 0)
                {
                    await _httpClient.DeleteAsync($"{BaseUrl}/clear-location/{userId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing location on logout: {ex.Message}");
            }
            finally
            {
                Preferences.Default.Clear();
            }
        }

        public bool IsLoggedIn => Preferences.Default.Get("is_logged_in", false);
        public bool IsOffline => Preferences.Default.Get("role", string.Empty) == "offline";
        public int UserId => Preferences.Default.Get("user_id", 0);
        public string Role => Preferences.Default.Get("role", string.Empty);
        public bool IsGuest => Role == "guest" || Role == "offline";
        public string Username => Preferences.Default.Get("username", string.Empty);
        public string Email => Preferences.Default.Get("email", string.Empty);

        // Static accessors for use without an instance
        public static string CurrentUsername => Preferences.Default.Get("username", string.Empty);
        public static int CurrentUserId => Preferences.Default.Get("user_id", 0);
    }

    public class AuthResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string role { get; set; }
    }
}
