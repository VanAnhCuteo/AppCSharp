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
    }
}