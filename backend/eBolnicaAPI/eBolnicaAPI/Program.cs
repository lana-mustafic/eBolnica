using eBolnicaAPI.Data;
using eBolnicaAPI.Extensions;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "eBolnica", Version = "v1" });

    // Include XML comments for Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Input JWT token in format: Bearer {your token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


builder.Services
    .AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<AppDbContext>();

// Configure DbContext with connection pooling and performance optimizations
var performanceSettings = builder.Configuration.GetSection("PerformanceSettings");
var enableQueryLogging = performanceSettings.GetValue<bool>("EnableQueryLogging", false);
var queryTimeout = performanceSettings.GetValue<int>("QueryTimeoutSeconds", 30);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("PharmacyIntegrationTests"));
}
else
{
    builder.Services.AddDbContextPool<AppDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions =>
            {
                sqlOptions.CommandTimeout(queryTimeout); // 30 second timeout
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        
        // Enable query logging in development only
        if (enableQueryLogging && builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.LogTo(Console.WriteLine, LogLevel.Information);
        }
    });
}

// Add in-memory caching for query results
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache entries
});

// Add response compression for large result sets
var enableCompression = performanceSettings.GetValue<bool>("EnableResponseCompression", true);
if (enableCompression)
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/json", "application/xml" });
    });
    
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Optimal;
    });
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
        ValidAudience = builder.Configuration["JwtConfig:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Key"]!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
        NameClaimType = JwtRegisteredClaimNames.Sub
    };
});
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:12383")
       .AllowAnyHeader()
       .AllowAnyMethod();
        });
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<IPharmacyAnalyticsService, PharmacyAnalyticsService>();
builder.Services.Configure<eBolnicaAPI.Models.Settings.MedicationImageUploadSettings>(
    builder.Configuration.GetSection(eBolnicaAPI.Models.Settings.MedicationImageUploadSettings.SectionName));

builder.Services.AddScoped<IMedicationImageFileValidator, MedicationImageFileValidator>();
builder.Services.AddScoped<IMedicationImageVirusScanner, MedicationImageVirusScanner>();
builder.Services.AddScoped<IMedicationImageOptimizer, MedicationImageOptimizer>();
builder.Services.AddScoped<IMedicationImageThumbnailGenerator, MedicationImageThumbnailGenerator>();
builder.Services.AddScoped<IMedicationImageStorageService, MedicationImageStorageService>();
builder.Services.AddScoped<IMedicationImageService, MedicationImageService>();

// Configure PDF generation settings
builder.Services.Configure<eBolnicaAPI.Models.Settings.PdfGenerationSettings>(
    builder.Configuration.GetSection("PdfSettings"));

// Add PDF report service
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (!app.Environment.IsEnvironment("Testing"))
    {
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        await DbInitializer.SeedPasswords(userManager);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

// Enable response compression
if (enableCompression)
{
    app.UseResponseCompression();
}

app.UseHttpsRedirection();

app.UseMedicationImageStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<AppUser>();

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
