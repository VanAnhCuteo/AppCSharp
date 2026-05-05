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
