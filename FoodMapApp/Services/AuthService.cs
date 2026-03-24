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
            Preferences.Default.Set("email", response.email); // Save email
            Preferences.Default.Set("role", response.role);
            Preferences.Default.Set("is_logged_in", true);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string username, string email, string? password = null)
        {
            try
            {
                var request = new { user_id = userId, username, email, password };
                var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/update", request);

                if (response.IsSuccessStatusCode)
                {
                    // Update local session
                    Preferences.Default.Set("username", username);
                    Preferences.Default.Set("email", email);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update Profile error: {ex.Message}");
                return false;
            }
        }

        public void Logout()
        {
            Preferences.Default.Clear();
        }

        public bool IsLoggedIn => Preferences.Default.Get("is_logged_in", false);
        public int UserId => Preferences.Default.Get("user_id", 0);
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
        public string email { get; set; } // Add email
        public string role { get; set; }
    }
}
