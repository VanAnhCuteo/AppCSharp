using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using FoodMapAPI.Models;
using FoodMapAPI.Services;

namespace FoodMapAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TranslationService _translator;

        public FoodController(IConfiguration configuration, TranslationService translator)
        {
            _configuration = configuration;
            _translator = translator;
        }

        private string? GetFullUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            if (relativePath.StartsWith("http")) return relativePath;
            
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            return $"{baseUrl}{(relativePath.StartsWith("/") ? "" : "/")}{relativePath}";
        }

        [HttpGet]
        public async Task<List<Food>> GetFoods([FromQuery] string lang = "vi", [FromQuery] int? category_id = null)
        {
            List<Food> foods = new List<Food>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    // Join with poi_guides to get description for the requested language, fallback to 'vi'
                    // Join with poi_qrs to get the QR code URL
                    string query = @"SELECT p.poi_id, p.category_id, p.name, p.address, p.latitude, p.longitude, p.open_time, p.range_meters,
                                   COALESCE(g.description, gv.description) as description, 
                                   (SELECT pi.image_url FROM poi_images pi WHERE pi.poi_id = p.poi_id ORDER BY pi.image_id ASC LIMIT 1) as image_url,
                                   pq.qr_code_url,
                                   (SELECT COUNT(*) FROM poi_audio_logs l WHERE l.poi_id = p.poi_id) as total_listens
                                   FROM pois p 
                                   LEFT JOIN poi_guides g ON p.poi_id = g.poi_id AND g.language = @lang
                                   LEFT JOIN poi_guides gv ON p.poi_id = gv.poi_id AND gv.language = 'vi'
                                   LEFT JOIN poi_qrs pq ON p.poi_id = pq.poi_id
                                   WHERE p.is_hidden = 0";

                    if (category_id.HasValue && category_id.Value > 0)
                    {
                        query += " AND p.category_id = @catId";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@lang", lang);
                    if (category_id.HasValue && category_id.Value > 0)
                    {
                        cmd.Parameters.AddWithValue("@catId", category_id.Value);
                    }
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                Food food = new Food
                                {
                                    id = Convert.ToInt32(reader["poi_id"]),
                                    category_id = reader["category_id"] != DBNull.Value ? Convert.ToInt32(reader["category_id"]) : 0,
                                    name = reader["name"].ToString() ?? "",
                                    description = reader["description"].ToString() ?? "",
                                    address = reader["address"].ToString() ?? "",
                                    latitude = reader["latitude"] != DBNull.Value ? Convert.ToDouble(reader["latitude"]) : 0.0,
                                    longitude = reader["longitude"] != DBNull.Value ? Convert.ToDouble(reader["longitude"]) : 0.0,
                                    open_time = reader["open_time"].ToString() ?? "",
                                    image_url = reader["image_url"] != DBNull.Value ? GetFullUrl(reader["image_url"].ToString()) : "",
                                    range_meters = reader["range_meters"] != DBNull.Value ? Convert.ToInt32(reader["range_meters"]) : 50,
                                    qr_code_url = reader["qr_code_url"] != DBNull.Value ? GetFullUrl(reader["qr_code_url"].ToString()) : null,
                                    total_listens = reader["total_listens"] != DBNull.Value ? Convert.ToInt32(reader["total_listens"]) : 0
                                };

                                if (lang != "vi")
                                {
                                    // food.name = await _translator.TranslateAsync(food.name, lang); // Don't translate brand names
                                    food.description = await _translator.TranslateAsync(food.description, lang);
                                    food.address = await _translator.TranslateAsync(food.address, lang);
                                }

                                foods.Add(food);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing record: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Error in GetFoods: {ex.Message}");
            }

            return foods;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFoodDetails(int id, [FromQuery] string lang = "vi")
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            FoodDetails details = new FoodDetails();
            details.images = new List<string>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    // 1. Get food info with description from poi_guides, fallback to 'vi'
                    string detailQuery = @"SELECT p.poi_id, p.category_id, p.name, p.address, p.latitude, p.longitude, p.open_time, p.range_meters,
                                         COALESCE(g.description, gv.description) as description, 
                                         (SELECT pi.image_url FROM poi_images pi WHERE pi.poi_id = p.poi_id ORDER BY pi.image_id ASC LIMIT 1) as image_url,
                                         pq.qr_code_url
                                         FROM pois p 
                                         LEFT JOIN poi_guides g ON p.poi_id = g.poi_id AND g.language = @lang 
                                         LEFT JOIN poi_guides gv ON p.poi_id = gv.poi_id AND gv.language = 'vi'
                                         LEFT JOIN poi_qrs pq ON p.poi_id = pq.poi_id
                                         WHERE p.poi_id = @id AND p.is_hidden = 0";

                    using (MySqlCommand cmd = new MySqlCommand(detailQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@lang", lang);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                details.id = Convert.ToInt32(reader["poi_id"]);
                                details.category_id = reader["category_id"] != DBNull.Value ? Convert.ToInt32(reader["category_id"]) : 0;
                                details.name = reader["name"].ToString() ?? "";
                                details.description = reader["description"].ToString() ?? "";
                                details.address = reader["address"].ToString() ?? "";
                                details.latitude = reader["latitude"] != DBNull.Value ? Convert.ToDouble(reader["latitude"]) : 0.0;
                                details.longitude = reader["longitude"] != DBNull.Value ? Convert.ToDouble(reader["longitude"]) : 0.0;
                                details.open_time = reader["open_time"].ToString() ?? "";
                                details.image_url = reader["image_url"] != DBNull.Value ? GetFullUrl(reader["image_url"].ToString()) : "";
                                details.range_meters = reader["range_meters"] != DBNull.Value ? Convert.ToInt32(reader["range_meters"]) : 50;
                                details.qr_code_url = reader["qr_code_url"] != DBNull.Value ? GetFullUrl(reader["qr_code_url"].ToString()) : null;
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }

                    if (lang != "vi")
                    {
                        // details.name = await _translator.TranslateAsync(details.name, lang); // Don't translate brand names
                        details.description = await _translator.TranslateAsync(details.description, lang);
                        details.address = await _translator.TranslateAsync(details.address, lang);
                    }



                    // 3. Get images (assuming poi_images table exists)
                    try
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT image_url FROM poi_images WHERE poi_id = @id ORDER BY image_id ASC", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    if (reader["image_url"] != DBNull.Value)
                                        details.images.Add(GetFullUrl(reader["image_url"].ToString()) ?? "");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching images for POI {id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Error in GetFoodDetails: {ex.Message}");
            }

            return Ok(details);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            List<Category> categories = new List<Category>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM categories WHERE is_hidden = 0", conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                categories.Add(new Category
                                {
                                    category_id = Convert.ToInt32(reader["category_id"]),
                                    category_name = reader["category_name"].ToString() ?? "",
                                    description = reader["description"].ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(categories);
        }



        [HttpPost("{id}/audio-log")]
        public async Task<IActionResult> LogAudio(int id, [FromBody] AudioLogRequest logReq)
        {
            if (logReq == null || logReq.duration_seconds <= 0) return BadRequest("Invalid duration");

            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "INSERT INTO poi_audio_logs (poi_id, user_id, duration_seconds) VALUES (@poi, @user, @duration)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@poi", id);
                        cmd.Parameters.AddWithValue("@user", logReq.user_id > 0 ? logReq.user_id : 1);
                        cmd.Parameters.AddWithValue("@duration", logReq.duration_seconds);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(new { success = true });
        }

        [HttpGet("stats/audio")]
        public async Task<IActionResult> GetAudioStats()
        {
            List<AudioStat> stats = new List<AudioStat>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT p.poi_id, p.name, 
                               COUNT(l.log_id) as total_listens, 
                               COALESCE(AVG(l.duration_seconds), 0) as avg_duration
                        FROM pois p
                        LEFT JOIN poi_audio_logs l ON p.poi_id = l.poi_id
                        GROUP BY p.poi_id, p.name
                        ORDER BY total_listens DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats.Add(new AudioStat
                                {
                                    poi_id = Convert.ToInt32(reader["poi_id"]),
                                    poi_name = reader["name"].ToString() ?? "",
                                    total_listens = Convert.ToInt32(reader["total_listens"]),
                                    avg_duration = Math.Round(Convert.ToDouble(reader["avg_duration"]), 1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(stats);
        }

        [HttpGet("{id}/guide")]
        public async Task<IActionResult> GetGuide(int id, [FromQuery] string lang = "vi")
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            Guide guide = null;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    
                    // Try to find the guide for the specific language first
                    string query = "SELECT * FROM poi_guides WHERE poi_id = @id AND language = @lang LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@lang", lang);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                guide = new Guide
                                {
                                    guide_id = Convert.ToInt32(reader["guide_id"]),
                                    poi_id = Convert.ToInt32(reader["poi_id"]),
                                    title = reader["title"].ToString() ?? "",
                                    description = reader["description"].ToString() ?? "",
                                    language = reader["language"].ToString() ?? "",
                                };
                            }
                        }
                    }

                    // Fallback to Vietnamese if requested language not found
                    if (guide == null && lang != "vi")
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM poi_guides WHERE poi_id = @id AND language = 'vi' LIMIT 1", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    guide = new Guide
                                    {
                                        guide_id = Convert.ToInt32(reader["guide_id"]),
                                        poi_id = Convert.ToInt32(reader["poi_id"]),
                                        title = reader["title"].ToString() ?? "",
                                        description = reader["description"].ToString() ?? "",
                                        language = reader["language"].ToString() ?? "",
                                    };
                                }
                            }
                        }

                        // Translate the Vietnamese fallback
                        if (guide != null)
                        {
                            try
                            {
                                guide.title = await _translator.TranslateAsync(guide.title, lang);
                                guide.description = await _translator.TranslateAsync(guide.description, lang);
                                guide.language = lang;
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Error in GetGuide: {ex.Message}");
            }

            if (guide != null)
            {
                return Ok(guide);
            }
            return NotFound();
        }

        [HttpGet("{id}/available-languages")]
        public async Task<IActionResult> GetAvailableLanguages(int id)
        {
            List<Language> languages = new List<Language>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT l.* 
                        FROM languages l
                        INNER JOIN poi_guides g ON l.language_code = g.language
                        WHERE g.poi_id = @id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                languages.Add(new Language
                                {
                                    language_code = reader["language_code"].ToString(),
                                    name = reader["name"].ToString(),
                                    flag_url = reader["flag_url"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(languages);
        }
        [HttpGet("audio-history/{userId}")]
        public async Task<IActionResult> GetAudioHistory(int userId)
        {
            List<PoiAudioLogDTO> history = new List<PoiAudioLogDTO>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT l.log_id, l.poi_id, p.name as poi_name, l.duration_seconds, l.created_at,
                               (SELECT pi.image_url FROM poi_images pi WHERE pi.poi_id = p.poi_id ORDER BY pi.image_id ASC LIMIT 1) as image_url
                        FROM poi_audio_logs l
                        INNER JOIN pois p ON l.poi_id = p.poi_id
                        WHERE l.user_id = @userId
                        ORDER BY l.created_at DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                history.Add(new PoiAudioLogDTO
                                {
                                    log_id = Convert.ToInt32(reader["log_id"]),
                                    poi_id = Convert.ToInt32(reader["poi_id"]),
                                    poi_name = reader["poi_name"].ToString() ?? "",
                                    poi_image_url = reader["image_url"] != DBNull.Value ? GetFullUrl(reader["image_url"].ToString()) : null,
                                    duration_seconds = Convert.ToInt32(reader["duration_seconds"]),
                                    created_at = Convert.ToDateTime(reader["created_at"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(history);
        }

        [HttpPost("translate-ui")]
        public async Task<IActionResult> TranslateUI([FromBody] TranslateUIRequest request)
        {
            if (request == null || request.Strings == null || string.IsNullOrEmpty(request.TargetLang))
                return BadRequest("Invalid request");

            var results = new Dictionary<string, string>();
            foreach (var item in request.Strings)
            {
                // Key is the local ID/key, Value is the source Vietnamese text
                results[item.Key] = await _translator.TranslateAsync(item.Value, request.TargetLang);
            }

            return Ok(results);
        }
    }

    public class TranslateUIRequest
    {
        public string TargetLang { get; set; } = "vi";
        public Dictionary<string, string> Strings { get; set; } = new();
    }
}
