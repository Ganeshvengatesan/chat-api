using ChatApplicationAPI.Data;
using ChatApplicationAPI.Hubs;
using ChatApplicationAPI.Interfaces;
using ChatApplicationAPI.Services;
using ChatApplicationAPI.Services.Encryption;
using ChatApplicationAPI.Services.Firebase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Neon PostgreSQL Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=ep-still-hill-azrvd8jc-pooler.c-3.ap-southeast-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_g0vB4tIuPzGk;Ssl Mode=Require;Trust Server Certificate=true;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Register Application Services & Firebase
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<IFirebaseService, FirebaseService>();
builder.Services.AddSingleton<IS3StorageService, S3StorageService>();
builder.Services.AddScoped<IDeviceTokenService, DeviceTokenService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IChatService, ChatService>();

// 3. Add Controllers & SignalR Realtime Hubs
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "LinkUp API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1Ni...\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// 4. Configure CORS for Flutter (Web, iOS, Android)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

// 5. Configure JWT Authentication
var secretKey = builder.Configuration["Jwt:Secret"] ?? "LinkUpSuperSecretEncryptionKey2026SecureJwtTokenKeyForChatApp!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "LinkUpAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "LinkUpApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

var app = builder.Build();

// 6. Ensure Database Created & Migrated on Neon PostgreSQL
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize Neon PostgreSQL database tables.");
    }
}

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LinkUp API v1");
    });
}

var wwwrootFolder = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
var uploadsFolder = Path.Combine(wwwrootFolder, "uploads");
if (!Directory.Exists(wwwrootFolder))
{
    Directory.CreateDirectory(wwwrootFolder);
}
if (!Directory.Exists(uploadsFolder))
{
    Directory.CreateDirectory(uploadsFolder);
}
builder.Environment.WebRootPath = wwwrootFolder;

var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".png"] = "image/png";
contentTypeProvider.Mappings[".jpg"] = "image/jpeg";
contentTypeProvider.Mappings[".jpeg"] = "image/jpeg";
contentTypeProvider.Mappings[".gif"] = "image/gif";
contentTypeProvider.Mappings[".webp"] = "image/webp";
contentTypeProvider.Mappings[".svg"] = "image/svg+xml";
contentTypeProvider.Mappings[".mp4"] = "video/mp4";
contentTypeProvider.Mappings[".mov"] = "video/quicktime";
contentTypeProvider.Mappings[".pdf"] = "application/pdf";
contentTypeProvider.Mappings[".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
contentTypeProvider.Mappings[".doc"] = "application/msword";
contentTypeProvider.Mappings[".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
contentTypeProvider.Mappings[".zip"] = "application/zip";
contentTypeProvider.Mappings[".mp3"] = "audio/mpeg";
contentTypeProvider.Mappings[".wav"] = "audio/wav";
contentTypeProvider.Mappings[".m4a"] = "audio/m4a";
contentTypeProvider.Mappings[".ogg"] = "audio/ogg";

app.UseCors("AllowAll");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsFolder),
    RequestPath = "/uploads",
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000");
    }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootFolder),
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
