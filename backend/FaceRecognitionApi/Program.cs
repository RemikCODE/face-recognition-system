using FaceRecognitionApi.Data;
using FaceRecognitionApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=face_recognition.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Services
builder.Services.AddScoped<CsvImportService>();
// DeepFace inference can take several minutes on the first run (model download + embedding).
// The default 100 s timeout is too short; allow up to 10 minutes.
builder.Services.AddHttpClient<IFaceRecognitionService, FaceRecognitionService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});

// Controllers + Razor Pages (web frontend served from the same host)
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Face Recognition API", Version = "v1" });
});

// CORS – allows the MAUI desktop/mobile app and web front-end to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Apply migrations, ensure database is created, and auto-seed from bundled CSV
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Auto-seed the Persons table from the bundled faces.csv if the table is empty
    if (!db.Persons.Any())
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Data", "faces.csv");
        if (File.Exists(csvPath))
        {
            var csvImport = scope.ServiceProvider.GetRequiredService<CsvImportService>();
            var count = await csvImport.ImportAsync(csvPath);
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Auto-seeded {Count} persons from bundled faces.csv", count);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Face Recognition API v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.MapControllers();
app.MapRazorPages();

app.Run();

// Expose Program for integration tests
public partial class Program { }
