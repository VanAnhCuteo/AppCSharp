using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FoodMapAdmin.Data;
using FoodMapAdmin.Components;
using FoodMapAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IUserService, UserService>();

// Configure MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
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
            db.Database.ExecuteSqlRaw("ALTER TABLE poi_image_pending_changes MODIFY poi_id INT NULL;");
        } catch { /* Already fixed or exists */ }

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


