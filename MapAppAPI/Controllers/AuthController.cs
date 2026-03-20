using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using FoodMapAPI.Models;
using System.Data;

namespace FoodMapAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.username) || string.IsNullOrEmpty(request.email) || string.IsNullOrEmpty(request.password))
            {
                return BadRequest(new AuthResponse { success = false, message = "Username, email, and password are required." });
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    // Check if user already exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username OR email = @email";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", request.username);
                        checkCmd.Parameters.AddWithValue("@email", request.email);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            return BadRequest(new AuthResponse { success = false, message = "Username or email already exists." });
                        }
                    }

                    // Insert new user
                    long userId = 0;
                    string insertQuery = "INSERT INTO users (username, email, password, role) VALUES (@username, @email, @password, 'user')";
                    using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@username", request.username);
                        insertCmd.Parameters.AddWithValue("@email", request.email);
                        insertCmd.Parameters.AddWithValue("@password", request.password); // In production, use Hashing!
                        await insertCmd.ExecuteNonQueryAsync();
                        userId = insertCmd.LastInsertedId;
                    }

                    return Ok(new AuthResponse
                    {
                        success = true,
                        message = "Registration successful.",
                        user_id = (int)userId,
                        username = request.username,
                        role = "user"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse { success = false, message = $"Database error: {ex.Message}" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.identifier) || string.IsNullOrEmpty(request.password))
            {
                return BadRequest(new AuthResponse { success = false, message = "Username/Email and password are required." });
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = "SELECT user_id, username, role FROM users WHERE (username = @id OR email = @id) AND password = @pass LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", request.identifier);
                        cmd.Parameters.AddWithValue("@pass", request.password);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Ok(new AuthResponse
                                {
                                    success = true,
                                    message = "Login successful.",
                                    user_id = Convert.ToInt32(reader["user_id"]),
                                    username = reader["username"].ToString(),
                                    role = reader["role"].ToString()
                                });
                            }
                            else
                            {
                                return Unauthorized(new AuthResponse { success = false, message = "Invalid username/email or password." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse { success = false, message = $"Database error: {ex.Message}" });
            }
        }
    }
}
