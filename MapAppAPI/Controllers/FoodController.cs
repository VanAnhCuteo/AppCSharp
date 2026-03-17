using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using FoodMapAPI.Models;

namespace FoodMapAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FoodController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public List<Food> GetFoods()
        {
            List<Food> foods = new List<Food>();

            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string query = "SELECT * FROM pois";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Food food = new Food
                    {
                        id = Convert.ToInt32(reader["poi_id"]),
                        category_id = Convert.ToInt32(reader["category_id"]),
                        name = reader["name"].ToString(),
                        description = reader["description"].ToString(),
                        address = reader["address"].ToString(),
                        latitude = Convert.ToDouble(reader["latitude"]),
                        longitude = Convert.ToDouble(reader["longitude"]),
                        open_time = reader["open_time"].ToString()
                    };

                    foods.Add(food);
                }
            }

            return foods;
        }

        [HttpGet("{id}")]
        public IActionResult GetFoodDetails(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            FoodDetails details = new FoodDetails();
            details.images = new List<string>();

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // 1. Get food info
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM pois WHERE poi_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            details.id = Convert.ToInt32(reader["poi_id"]);
                            details.category_id = reader["category_id"] != DBNull.Value ? Convert.ToInt32(reader["category_id"]) : 0;
                            details.name = reader["name"].ToString();
                            details.description = reader["description"].ToString();
                            details.address = reader["address"].ToString();
                            details.latitude = reader["latitude"] != DBNull.Value ? Convert.ToDouble(reader["latitude"]) : 0.0;
                            details.longitude = reader["longitude"] != DBNull.Value ? Convert.ToDouble(reader["longitude"]) : 0.0;
                            details.open_time = reader["open_time"].ToString();
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }

                // 2. Get visitor count
                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM poi_visits WHERE poi_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    details.visitor_count = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 3. Get images (assuming poi_images table exists)
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT image_url FROM poi_images WHERE poi_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                details.images.Add(reader["image_url"].ToString());
                            }
                        }
                    }
                }
                catch
                {
                    /* Ignore if table doesn't exist */
                }
            }
            return Ok(details);
        }

        [HttpGet("{id}/reviews")]
        public IActionResult GetReviews(int id)
        {
            List<Review> reviews = new List<Review>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM poi_reviews WHERE poi_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                reviews.Add(new Review
                                {
                                    id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                    poi_id = reader["poi_id"] != DBNull.Value ? Convert.ToInt32(reader["poi_id"]) : 0,
                                    user_id = reader["user_id"] != DBNull.Value ? Convert.ToInt32(reader["user_id"]) : 0,
                                    rating = reader["rating"] != DBNull.Value ? Convert.ToInt32(reader["rating"]) : 5,
                                    comment = reader["comment"].ToString(),
                                    created_at = reader["created_at"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch { } // Ignore missing table
            return Ok(reviews);
        }

        [HttpPost("{id}/reviews")]
        public IActionResult AddReview(int id, [FromBody] Review review)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    // Assume table poi_reviews(poi_id, user_id, rating, comment)
                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO poi_reviews (poi_id, user_id, rating, comment) VALUES (@poi, @user, @rating, @comment)", conn))
                    {
                        cmd.Parameters.AddWithValue("@poi", id);
                        cmd.Parameters.AddWithValue("@user", review.user_id > 0 ? review.user_id : 1);
                        cmd.Parameters.AddWithValue("@rating", review.rating > 0 ? review.rating : 5);
                        cmd.Parameters.AddWithValue("@comment", review.comment ?? "");
                        cmd.ExecuteNonQuery();
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
        public IActionResult LogVisit(int id, [FromBody] Visit visitReq)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO poi_visits (poi_id, user_id) VALUES (@poi, @user)", conn))
                    {
                        cmd.Parameters.AddWithValue("@poi", id);
                        cmd.Parameters.AddWithValue("@user", visitReq != null && visitReq.user_id > 0 ? visitReq.user_id : 1);
                        cmd.ExecuteNonQuery();
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
        public IActionResult GetGuide(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            Guide guide = null;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM poi_guides WHERE poi_id = @id LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                guide = new Guide
                                {
                                    guide_id = Convert.ToInt32(reader["guide_id"]),
                                    poi_id = Convert.ToInt32(reader["poi_id"]),
                                    title = reader["title"].ToString(),
                                    description = reader["description"].ToString(),
                                    language = reader["language"].ToString(),
                                };
                            }
                        }
                    }
                }
            }
            catch { }

            if (guide != null) return Ok(guide);
            return NotFound();
        }
    }
}