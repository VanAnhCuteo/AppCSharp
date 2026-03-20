namespace FoodMapAPI.Models
{
    public class User
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string role { get; set; } // 'user', 'admin', 'CNH'
        public DateTime created_at { get; set; }
    }

    public class LoginRequest
    {
        public string identifier { get; set; } // username or email
        public string password { get; set; }
    }

    public class RegisterRequest
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
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
