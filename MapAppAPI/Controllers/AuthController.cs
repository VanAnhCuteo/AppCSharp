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

                    string query = "SELECT user_id, username, role, email, is_blocked FROM users WHERE (username = @id OR email = @id) AND password = @pass LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", request.identifier);
                        cmd.Parameters.AddWithValue("@pass", request.password);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                bool isBlocked = Convert.ToBoolean(reader["is_blocked"]);
                                if (isBlocked)
                                {
                                    return Unauthorized(new AuthResponse { success = false, message = "Tài khoản của bạn đã bị khóa." });
                                }

                                return Ok(new AuthResponse
                                {
                                    success = true,
                                    message = "Login successful.",
                                    user_id = Convert.ToInt32(reader["user_id"]),
                                    username = reader["username"].ToString(),
                                    role = reader["role"].ToString(),
                                    email = reader["email"].ToString()
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

        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (request.user_id <= 0 || string.IsNullOrEmpty(request.username) || string.IsNullOrEmpty(request.email))
            {
                return BadRequest(new { success = false, message = "User ID, username, and email are required." });
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    // Check if new username or email already exists for OTHER users
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE (username = @username OR email = @email) AND user_id != @userId";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", request.username);
                        checkCmd.Parameters.AddWithValue("@email", request.email);
                        checkCmd.Parameters.AddWithValue("@userId", request.user_id);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            return BadRequest(new { success = false, message = "Username or email already exists." });
                        }
                    }

                    // Update user
                    string updateQuery = "UPDATE users SET username = @username, email = @email" +
                                       (!string.IsNullOrEmpty(request.password) ? ", password = @password" : "") +
                                       " WHERE user_id = @userId";

                    using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@username", request.username);
                        updateCmd.Parameters.AddWithValue("@email", request.email);
                        updateCmd.Parameters.AddWithValue("@userId", request.user_id);
                        if (!string.IsNullOrEmpty(request.password))
                        {
                            updateCmd.Parameters.AddWithValue("@password", request.password);
                        }

                        int affectedRows = await updateCmd.ExecuteNonQueryAsync();
                        if (affectedRows > 0)
                        {
                            return Ok(new { success = true, message = "Profile updated successfully.", username = request.username, email = request.email });
                        }
                        else
                        {
                            return NotFound(new { success = false, message = "User not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {ex.Message}" });
            }
        }
        [HttpPost("update-location")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateRequest request)
        {
            if (request.user_id == 0) return BadRequest(new { success = false, message = "Invalid user ID" });

            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    // Upsert logic: Update if exists, Insert if not
                    string query = @"
                        INSERT INTO user_locations (user_id, latitude, longitude, last_active, is_listening_audio, listening_poi_id) 
                        VALUES (@userId, @lat, @lng, NOW(), @isListening, @poiId)
                        ON DUPLICATE KEY UPDATE 
                        latitude = @lat, 
                        longitude = @lng, 
                        last_active = NOW(),
                        is_listening_audio = @isListening,
                        listening_poi_id = @poiId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", request.user_id);
                        cmd.Parameters.AddWithValue("@lat", request.latitude);
                        cmd.Parameters.AddWithValue("@lng", request.longitude);
                        cmd.Parameters.AddWithValue("@isListening", request.is_listening);
                        cmd.Parameters.AddWithValue("@poiId", (object)request.poi_id ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {ex.Message}" });
            }
        }
        [HttpDelete("clear-location/{userId}")]
        public async Task<IActionResult> ClearLocation(int userId)
        {
            if (userId == 0) return BadRequest(new { success = false, message = "Invalid user ID" });

            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = "DELETE FROM user_locations WHERE user_id = @userId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { success = true, message = "Location cleared." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {ex.Message}" });
            }
        }
    }
}
