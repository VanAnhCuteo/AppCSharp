using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using FoodMapAPI.Models;
using FoodMapAPI.Services;

namespace FoodMapAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TourController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TranslationService _translator;

        public TourController(IConfiguration configuration, TranslationService translator)
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
        public async Task<List<Tour>> GetTours(
            [FromQuery] string? search = null,
            [FromQuery] int? min_duration = null, 
            [FromQuery] int? max_duration = null,
            [FromQuery] decimal? min_price = null,
            [FromQuery] decimal? max_price = null)
        {
            List<Tour> tours = new List<Tour>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT DISTINCT t.* 
                        FROM tours t
                        LEFT JOIN tour_pois tp ON t.tour_id = tp.tour_id
                        LEFT JOIN pois p ON tp.poi_id = p.poi_id";

                    List<string> filters = new List<string>();
                    
                    if (!string.IsNullOrEmpty(search)) 
                    {
                        filters.Add(@"(t.name LIKE @search 
                                   OR t.description LIKE @search 
                                   OR p.name LIKE @search 
                                   OR CAST(t.price AS CHAR) LIKE @search 
                                   OR CAST(t.duration_minutes AS CHAR) LIKE @search)");
                    }
                    if (min_duration.HasValue) filters.Add("t.duration_minutes >= @min_dur");
                    if (max_duration.HasValue) filters.Add("t.duration_minutes <= @max_dur");
                    if (min_price.HasValue) filters.Add("t.price >= @min_price");
                    if (max_price.HasValue) filters.Add("t.price <= @max_price");

                    if (filters.Count > 0)
                    {
                        query += " WHERE " + string.Join(" AND ", filters);
                    }

                    query += " ORDER BY t.created_at DESC";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    if (min_duration.HasValue) cmd.Parameters.AddWithValue("@min_dur", min_duration.Value);
                    if (max_duration.HasValue) cmd.Parameters.AddWithValue("@max_dur", max_duration.Value);
                    if (min_price.HasValue) cmd.Parameters.AddWithValue("@min_price", min_price.Value);
                    if (max_price.HasValue) cmd.Parameters.AddWithValue("@max_price", max_price.Value);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tours.Add(new Tour
                            {
                                tour_id = reader.GetInt32(reader.GetOrdinal("tour_id")),
                                name = reader["name"]?.ToString() ?? "",
                                description = reader["description"]?.ToString() ?? "",
                                duration_minutes = reader.GetInt32(reader.GetOrdinal("duration_minutes")),
                                price = reader.GetDecimal(reader.GetOrdinal("price")),
                                created_at = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Error in GetTours: {ex.Message}");
            }

            return tours;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTourDetails(int id, [FromQuery] string lang = "vi")
        {
            Tour? tour = null;
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    // 1. Get Tour Metadata
                    string tourQuery = "SELECT * FROM tours WHERE tour_id = @id";
                    using (MySqlCommand cmd = new MySqlCommand(tourQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                tour = new Tour
                                {
                                    tour_id = reader.GetInt32(reader.GetOrdinal("tour_id")),
                                    name = reader["name"]?.ToString() ?? "",
                                    description = reader["description"]?.ToString() ?? "",
                                    duration_minutes = reader.GetInt32(reader.GetOrdinal("duration_minutes")),
                                    price = reader.GetDecimal(reader.GetOrdinal("price")),
                                    created_at = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    pois = new List<TourPoiDetail>()
                                };
                            }
                        }
                    }

                    if (tour == null) return NotFound("Tour không tồn tại.");

                    // Translate tour info if not Vietnamese
                    if (lang != "vi" && !string.IsNullOrEmpty(tour.name))
                    {
                        tour.name = await _translator.TranslateAsync(tour.name, lang);
                        if (!string.IsNullOrEmpty(tour.description))
                            tour.description = await _translator.TranslateAsync(tour.description, lang);
                    }

                    // 2. Get POIs in this tour
                    // Note: image_url is in poi_images table, not pois table
                    string poiQuery = @"
                        SELECT tp.*, p.name, p.address, p.latitude, p.longitude, p.open_time,
                               (SELECT pi.image_url FROM poi_images pi WHERE pi.poi_id = p.poi_id ORDER BY pi.image_id ASC LIMIT 1) as poi_image_url,
                               COALESCE(g.description, gv.description) as poi_description
                        FROM tour_pois tp
                        JOIN pois p ON tp.poi_id = p.poi_id
                        LEFT JOIN poi_guides g ON p.poi_id = g.poi_id AND g.language = @lang
                        LEFT JOIN poi_guides gv ON p.poi_id = gv.poi_id AND gv.language = 'vi'
                        WHERE tp.tour_id = @id
                        ORDER BY tp.sequence_order ASC";

                    using (MySqlCommand cmd = new MySqlCommand(poiQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@lang", lang);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var poi = new TourPoiDetail
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("poi_id")),
                                    name = reader["name"]?.ToString() ?? "",
                                    address = reader["address"]?.ToString() ?? "",
                                    latitude = Convert.ToDouble(reader["latitude"]),
                                    longitude = Convert.ToDouble(reader["longitude"]),
                                    image_url = reader["poi_image_url"] != DBNull.Value ? GetFullUrl(reader["poi_image_url"].ToString()) : "",
                                    open_time = reader["open_time"]?.ToString() ?? "",
                                    description = reader["poi_description"]?.ToString() ?? "",
                                    sequence_order = reader.GetInt32(reader.GetOrdinal("sequence_order")),
                                    stay_duration = reader.GetInt32(reader.GetOrdinal("stay_duration")),
                                    average_price = reader.GetDecimal(reader.GetOrdinal("average_price"))
                                };

                                if (lang != "vi" && !string.IsNullOrEmpty(poi.address))
                                {
                                    poi.address = await _translator.TranslateAsync(poi.address, lang);
                                    if (!string.IsNullOrEmpty(poi.description))
                                        poi.description = await _translator.TranslateAsync(poi.description, lang);
                                }

                                tour.pois.Add(poi);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Lỗi dữ liệu: " + ex.Message);
            }

            return Ok(tour);
        }

        // --- Admin Management ---

        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] Tour tour)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "INSERT INTO tours (name, description, duration_minutes, price) VALUES (@name, @desc, @dur, @price); SELECT LAST_INSERT_ID();";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", tour.name);
                        cmd.Parameters.AddWithValue("@desc", tour.description);
                        cmd.Parameters.AddWithValue("@dur", tour.duration_minutes);
                        cmd.Parameters.AddWithValue("@price", tour.price);
                        var id = await cmd.ExecuteScalarAsync();
                        return Ok(new { tour_id = id });
                    }
                }
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] Tour tour)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "UPDATE tours SET name = @name, description = @desc, duration_minutes = @dur, price = @price WHERE tour_id = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@name", tour.name);
                        cmd.Parameters.AddWithValue("@desc", tour.description);
                        cmd.Parameters.AddWithValue("@dur", tour.duration_minutes);
                        cmd.Parameters.AddWithValue("@price", tour.price);
                        await cmd.ExecuteNonQueryAsync();
                        return Ok();
                    }
                }
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("{id}/pois")]
        public async Task<IActionResult> AddPoiToTour(int id, [FromBody] TourPoiDetail detail)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "INSERT INTO tour_pois (tour_id, poi_id, sequence_order, stay_duration, average_price) VALUES (@tid, @pid, @seq, @stay, @price)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", id);
                        cmd.Parameters.AddWithValue("@pid", detail.id);
                        cmd.Parameters.AddWithValue("@seq", detail.sequence_order);
                        cmd.Parameters.AddWithValue("@stay", detail.stay_duration);
                        cmd.Parameters.AddWithValue("@price", detail.average_price);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}/pois/{poiId}")]
        public async Task<IActionResult> RemovePoiFromTour(int id, int poiId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "DELETE FROM tour_pois WHERE tour_id = @tid AND poi_id = @pid";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tid", id);
                        cmd.Parameters.AddWithValue("@pid", poiId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "DELETE FROM tours WHERE tour_id = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
