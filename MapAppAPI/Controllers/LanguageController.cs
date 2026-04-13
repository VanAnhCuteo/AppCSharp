using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using FoodMapAPI.Models;

namespace FoodMapAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguageController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LanguageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<List<Language>> GetLanguages()
        {
            List<Language> languages = new List<Language>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    
                    // Create table if not exists
                    string createTable = @"CREATE TABLE IF NOT EXISTS languages (
                        language_code VARCHAR(10) PRIMARY KEY, 
                        name VARCHAR(50) NOT NULL, 
                        flag_url VARCHAR(255)
                    );";
                    using (var createCmd = new MySqlCommand(createTable, conn)) await createCmd.ExecuteNonQueryAsync();

                    // Seed default languages if empty
                    string checkCount = "SELECT COUNT(*) FROM languages";
                    using (var countCmd = new MySqlCommand(checkCount, conn))
                    {
                        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            string seed = @"INSERT INTO languages (language_code, name, flag_url) VALUES 
                                ('vi', 'Tiếng Việt', 'vn_flag.png'),
                                ('en', 'English', 'uk_flag.png'),
                                ('zh', '中文', 'cn_flag.png'),
                                ('ko', '한국어', 'kr_flag.png'),
                                ('ja', '日本語', 'jp_flag.png');";
                            using (var seedCmd = new MySqlCommand(seed, conn)) await seedCmd.ExecuteNonQueryAsync();
                        }
                    }

                    string query = "SELECT * FROM languages";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
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
                Console.WriteLine($"Error in GetLanguages: {ex.Message}");
            }
            return languages;
        }

        [HttpPost]
        public async Task<IActionResult> AddLanguage([FromBody] Language lang)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "INSERT INTO languages (language_code, name, flag_url) VALUES (@code, @name, @flag)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", lang.language_code);
                        cmd.Parameters.AddWithValue("@name", lang.name);
                        cmd.Parameters.AddWithValue("@flag", lang.flag_url ?? "");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> DeleteLanguage(string code)
        {
            if (code == "vi") return BadRequest("Cannot delete default language");

            string connStr = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "DELETE FROM languages WHERE language_code = @code";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", code);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
