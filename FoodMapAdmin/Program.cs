using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FoodMapAdmin.Data;
using FoodMapAdmin.Components;
using FoodMapAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IUserService, UserService>();

// Configure MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add custom services
builder.Services.AddScoped<IPoiService, PoiService>();
builder.Services.AddScoped<IPoiPendingChangeService, PoiPendingChangeService>();
builder.Services.AddScoped<IPoiImagePendingChangeService, PoiImagePendingChangeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPoiGuideService, PoiGuideService>();
builder.Services.AddScoped<IPoiImageService, PoiImageService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITranslationService, TranslationService>();

builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IPoiGuidePendingChangeService, PoiGuidePendingChangeService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITourService, TourService>();


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:5173")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = "Cookies"; // Default for Web UI
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("OwnerOnly", policy => policy.RequireRole("CNH", "admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

// Serve images from the mobile app's Resources/Raw/images directory
var imagePath = builder.Configuration["ImageStoragePath"];
if (!string.IsNullOrEmpty(imagePath) && Directory.Exists(imagePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagePath),
        RequestPath = "/images"
    });
}

app.UseAntiforgery();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Seed Admin for testing
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Schema Fix for Image Moderation
        try {
            // Chỉ chạy lệnh MODIFY nếu cột poi_id chưa cho phép NULL (tránh log lỗi vô ích)
            var columnInfo = db.Database.SqlQueryRaw<string>(
                "SELECT IS_NULLABLE FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'poi_image_pending_changes' AND column_name = 'poi_id'"
            ).ToList();

            if (columnInfo.Any() && columnInfo[0].ToUpper() == "NO")
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE poi_image_pending_changes MODIFY poi_id INT NULL;");
            }
        } catch { /* Suppress noisy logs */ }



        // Schema Fix for Languages Table
        try {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS `languages` (
                  `language_code` varchar(20) NOT NULL,
                  `name` varchar(100) NOT NULL,
                  `flag_url` varchar(255) DEFAULT NULL,
                  PRIMARY KEY (`language_code`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
            ");
            
            // Schema Fix for Audio Pending Changes
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS `poi_guide_pending_changes` (
                  `change_id` INT NOT NULL AUTO_INCREMENT,
                  `poi_id` INT NOT NULL,
                  `guide_id` INT DEFAULT NULL,
                  `change_type` VARCHAR(20) NOT NULL,
                  `title` VARCHAR(200) DEFAULT NULL,
                  `description` TEXT,
                  `language` VARCHAR(20) DEFAULT 'vi',
                  `user_id` INT DEFAULT NULL,
                  `status` VARCHAR(20) DEFAULT 'pending',
                  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`change_id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
            ");

            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS `app_notifications` (
                  `id` INT NOT NULL AUTO_INCREMENT,
                  `user_id` INT NOT NULL,
                  `title` VARCHAR(255) NOT NULL,
                  `message` TEXT NOT NULL,
                  `type` VARCHAR(50) DEFAULT 'info',
                  `is_read` TINYINT(1) DEFAULT 0,
                  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  `category` VARCHAR(50) DEFAULT NULL,
                  PRIMARY KEY (`id`),
                  KEY `idx_user_read` (`user_id`, `is_read`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");
            
            // Seed default languages if empty
            var langCount = db.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM languages").ToList().FirstOrDefault();
            if (langCount == 0)
            {
                db.Database.ExecuteSqlRaw(@"
                    INSERT INTO languages (language_code, name) VALUES 
                    ('vi', 'Vietnamese (Tiếng Việt)'),
                    ('en', 'English'),
                    ('zh', 'Chinese (Mandarin)'),
                    ('ko', 'Korean'),
                    ('ja', 'Japanese');
                ");
            }
        } catch (Exception ex) { 
            Console.WriteLine($"[Startup] Languages Table Fix Error: {ex.Message}");
        }

        // Schema Fix for Guest Support: Remove FK on user_locations
        try {
            db.Database.ExecuteSqlRaw(@"
                SET @constraint_name = (SELECT CONSTRAINT_NAME 
                                       FROM information_schema.KEY_COLUMN_USAGE 
                                       WHERE TABLE_SCHEMA = DATABASE() 
                                       AND TABLE_NAME = 'user_locations' 
                                       AND COLUMN_NAME = 'user_id' 
                                       AND REFERENCED_TABLE_NAME IS NOT NULL);
                SET @sql = IF(@constraint_name IS NOT NULL, 
                              CONCAT('ALTER TABLE user_locations DROP FOREIGN KEY ', @constraint_name), 
                              'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        } catch (Exception ex) {
            Console.WriteLine($"[Startup] Guest ID Support Fix Error: {ex.Message}");
        }

        // Schema for Tours
        try {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS `tours` (
                  `id` INT NOT NULL AUTO_INCREMENT,
                  `name` VARCHAR(200) NOT NULL,
                  `description` TEXT,
                  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
            ");

            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS `tour_pois` (
                  `id` INT NOT NULL AUTO_INCREMENT,
                  `tour_id` INT NOT NULL,
                  `poi_id` INT NOT NULL,
                  `stay_duration_minutes` INT DEFAULT 30,
                  `approximate_price` VARCHAR(100) DEFAULT NULL,
                  `order_index` INT DEFAULT 0,
                  PRIMARY KEY (`id`),
                  KEY `idx_tour_poi` (`tour_id`, `poi_id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS `tour_histories` (
                  `id` INT NOT NULL AUTO_INCREMENT,
                  `user_id` INT NOT NULL,
                  `tour_id` INT NOT NULL,
                  `status` VARCHAR(50) DEFAULT 'InProgress',
                  `progress_percentage` DECIMAL(5,2) DEFAULT 0,
                  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`id`),
                  KEY `idx_user_tour` (`user_id`, `tour_id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");
        } catch (Exception ex) {
            Console.WriteLine($"[Startup] Tours Table Fix Error: {ex.Message}");
        }

        var user = db.Users.FirstOrDefault(u => u.Username == "vananh");
        if (user != null && user.Role != "admin")
        {
            user.Role = "admin";
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Error during database initialization: {ex.Message}");
        // Don't rethrow, let the app start so we can see the UI/logs
    }
}

app.Run();


