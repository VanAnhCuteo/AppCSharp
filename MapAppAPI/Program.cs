using FoodMapAPI.Services;
using FoodMapAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOriginsPolicy",
        policy =>
        {
            policy.AllowAnyOrigin() // Caution: Use only for development
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TranslationService>();

// Register EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register Tour Service
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

// Serve images from the configured physical directory
var imagePath = builder.Configuration["ImageStoragePath"];
if (!string.IsNullOrEmpty(imagePath) && Directory.Exists(imagePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagePath),
        RequestPath = "/images"
    });
}

app.UseAuthorization();
app.UseCors("AllowAllOriginsPolicy");
app.MapControllers();

// Database Seeding/Migration for Tours
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS `tours` (
                `id` INT NOT NULL AUTO_INCREMENT,
                `name` VARCHAR(200) NOT NULL,
                `description` TEXT,
                `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (`id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS `tour_pois` (
                `id` INT NOT NULL AUTO_INCREMENT,
                `tour_id` INT NOT NULL,
                `poi_id` INT NOT NULL,
                `stay_duration_minutes` INT DEFAULT 30,
                `approximate_price` VARCHAR(100) DEFAULT NULL,
                `order_index` INT DEFAULT 0,
                PRIMARY KEY (`id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Database Migration Error: {ex.Message}");
    }
}

app.Run("http://0.0.0.0:5000");