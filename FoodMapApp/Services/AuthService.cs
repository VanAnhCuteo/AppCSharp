using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace FoodMapApp.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://10.0.2.2:5000/api/auth"; // For Android Emulator (using localhost of host machine)
        // Note: Change to the actual IP for physical devices.

        public AuthService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<AuthResponse> LoginAsync(string identifier, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", new { identifier, password });
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (result != null && result.success)
                {
                    SaveUserSession(result);
                }

                return result ?? new AuthResponse { success = false, message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                return new AuthResponse { success = false, message = $"Connection error: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> RegisterAsync(string username, string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", new { username, email, password });
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (result != null && result.success)
                {
                    SaveUserSession(result);
                }

                return result ?? new AuthResponse { success = false, message = "Empty response from server" };
            }
            catch (Exception ex)
            {
                return new AuthResponse { success = false, message = $"Connection error: {ex.Message}" };
            }
        }

        private void SaveUserSession(AuthResponse response)
        {
            Preferences.Default.Set("user_id", response.user_id);
            Preferences.Default.Set("username", response.username);
            Preferences.Default.Set("role", response.role);
            Preferences.Default.Set("is_logged_in", true);
        }

        public void Logout()
        {
            Preferences.Default.Clear();
        }

        public bool IsLoggedIn => Preferences.Default.Get("is_logged_in", false);
        public int UserId => Preferences.Default.Get("user_id", 0);
        public string Username => Preferences.Default.Get("username", string.Empty);

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
        public string role { get; set; }
    }
}
