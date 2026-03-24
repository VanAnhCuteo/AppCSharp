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

                    // Select all from pois. We'll filter or just take the first language we find.
                    string query = "SELECT p.*, (SELECT pi.image_url FROM poi_images pi WHERE pi.poi_id = p.poi_id ORDER BY pi.image_id ASC LIMIT 1) as image_url FROM pois p";

                    if (category_id.HasValue && category_id.Value > 0)
                    {
                        query += " WHERE p.category_id = @catId";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, conn);
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
                                    image_url = reader["image_url"] != DBNull.Value ? reader["image_url"].ToString() : "",
                                    range_meters = reader["range_meters"] != DBNull.Value ? Convert.ToInt32(reader["range_meters"]) : 50
                                };

                                if (lang != "vi")
                                {
                                    food.name = await _translator.TranslateAsync(food.name, lang);
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

                    // 1. Get food info
                    using (MySqlCommand cmd = new MySqlCommand("SELECT p.*, (SELECT pi.image_url FROM poi_images pi WHERE pi.poi_id = p.poi_id ORDER BY pi.image_id ASC LIMIT 1) as image_url FROM pois p WHERE p.poi_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
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
                                details.image_url = reader["image_url"] != DBNull.Value ? reader["image_url"].ToString() : "";
                                details.range_meters = reader["range_meters"] != DBNull.Value ? Convert.ToInt32(reader["range_meters"]) : 50;
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }

                    if (lang != "vi")
                    {
                        details.name = await _translator.TranslateAsync(details.name, lang);
                        details.description = await _translator.TranslateAsync(details.description, lang);
                        details.address = await _translator.TranslateAsync(details.address, lang);
                    }

                    // 2. Get unique visitor count
                    using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(DISTINCT user_id) FROM poi_visits WHERE poi_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        details.visitor_count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
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
                                        details.images.Add(reader["image_url"].ToString() ?? "");
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM categories", conn))
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

        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetReviews(int id)
        {
            List<Review> reviews = new List<Review>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM reviews WHERE poi_id = @id ORDER BY created_at DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reviews.Add(new Review
                                {
                                    id = reader["review_id"] != DBNull.Value ? Convert.ToInt32(reader["review_id"]) : 0,
                                    poi_id = reader["poi_id"] != DBNull.Value ? Convert.ToInt32(reader["poi_id"]) : 0,
                                    user_id = reader["user_id"] != DBNull.Value ? Convert.ToInt32(reader["user_id"]) : 0,
                                    rating = reader["rating"] != DBNull.Value ? Convert.ToInt32(reader["rating"]) : 5,
                                    comment = reader["comment"].ToString() ?? "",
                                    created_at = reader["created_at"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Error in GetReviews: {ex.Message}");
            }
            return Ok(reviews);
        }

        [HttpPost("{id}/reviews")]
        public async Task<IActionResult> AddReview(int id, [FromBody] Review review)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    // Table: reviews(poi_id, user_id, rating, comment)
                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO reviews (poi_id, user_id, rating, comment) VALUES (@poi, @user, @rating, @comment)", conn))
                    {
                        cmd.Parameters.AddWithValue("@poi", id);
                        cmd.Parameters.AddWithValue("@user", review.user_id > 0 ? review.user_id : 1);
                        cmd.Parameters.AddWithValue("@rating", review.rating > 0 ? review.rating : 5);
                        cmd.Parameters.AddWithValue("@comment", review.comment ?? "");
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

        [HttpPost("{id}/visit")]
        public async Task<IActionResult> LogVisit(int id, [FromBody] Visit visitReq)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    // Only insert if not already visited by this user
                    string query = @"INSERT INTO poi_visits (poi_id, user_id) 
                                   SELECT @poi, @user 
                                   WHERE NOT EXISTS (SELECT 1 FROM poi_visits WHERE poi_id = @poi AND user_id = @user)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@poi", id);
                        cmd.Parameters.AddWithValue("@user", visitReq != null && visitReq.user_id > 0 ? visitReq.user_id : 1);
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM poi_guides WHERE poi_id = @id LIMIT 1", conn))
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Error in GetGuide: {ex.Message}");
            }

            if (guide != null)
            {
                if (lang != "vi")
                {
                    try
                    {
                        guide.title = await _translator.TranslateAsync(guide.title, lang);
                        guide.description = await _translator.TranslateAsync(guide.description, lang);
                        guide.language = lang;
                    }
                    catch { }
                }
                return Ok(guide);
            }
            return NotFound();
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetVisitHistory(int userId)
        {
            List<object> history = new List<object>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    // Join poi_visits with pois to get name and address
                    string query = @"SELECT p.poi_id, p.name, p.address 
                                   FROM poi_visits v 
                                   JOIN pois p ON v.poi_id = p.poi_id 
                                   WHERE v.user_id = @userId 
                                   ORDER BY v.visit_date DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                history.Add(new
                                {
                                    id = Convert.ToInt32(reader["poi_id"]),
                                    name = reader["name"].ToString(),
                                    address = reader["address"].ToString()
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

        [HttpGet("reviews/user/{userId}")]
        public async Task<IActionResult> GetUserReviews(int userId)
        {
            List<Review> reviews = new List<Review>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM reviews WHERE user_id = @user ORDER BY created_at DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@user", userId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                reviews.Add(new Review
                                {
                                    id = Convert.ToInt32(reader["review_id"]),
                                    poi_id = Convert.ToInt32(reader["poi_id"]),
                                    user_id = Convert.ToInt32(reader["user_id"]),
                                    rating = Convert.ToInt32(reader["rating"]),
                                    comment = reader["comment"].ToString() ?? "",
                                    created_at = reader["created_at"]?.ToString()
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
            return Ok(reviews);
        }
    }
}