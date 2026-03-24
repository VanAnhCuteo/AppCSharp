using FoodMapAPI.Services;

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
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TranslationService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAuthorization();
app.UseCors("AllowAllOriginsPolicy");
app.MapControllers();

app.Run("http://0.0.0.0:5000");