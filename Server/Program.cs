using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

using FarmerQuest.Server.Configuration;
using FarmerQuest.Server.Databases;
using FarmerQuest.Server.Databases.Seed;
using FarmerQuest.Server.Options;
using FarmerQuest.Server.Features.Auth;
using FarmerQuest.Server.Features.Social;
using FarmerQuest.Server.Features.Users;
using FarmerQuest.Server.Security;
using FarmerQuest.Server.GameFeatures.GameSessions;
using FarmerQuest.Server.GameFeatures.Farm;
using FarmerQuest.Server.Features.PlayerStats;

// Base .env, then optional local overrides (clobber)
Env.Load();
if (File.Exists(".env.local"))
    Env.Load(".env.local", new DotNetEnv.LoadOptions(setEnvVars: true, clobberExistingVars: true));

var builder = WebApplication.CreateBuilder(args);

// Machine-specific Development overrides (gitignored)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<PlayerProgressOptions>(
    builder.Configuration.GetSection(PlayerProgressOptions.SectionName));

// Fail fast if JWT key is missing or still the placeholder
JwtOptions jwtBind = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtBind.Key) || jwtBind.Key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Konfiguration mangler gyldig Jwt:Key. Sæt env variablen JWT__KEY.");

if (jwtBind.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key skal vaere mindst 32 tegn.");

// Resolve SQL connection (explicit config or Development auto-detect)
using var bootstrapLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("Database");
string sqlConnection = DatabaseConnectionResolver.Resolve(
    builder.Configuration,
    builder.Environment,
    bootstrapLogger);

builder.Services.AddDbContext<FarmerQuestDbContext>(options =>
    options.UseSqlServer(sqlConnection));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtBind.Issuer,
            ValidAudience = jwtBind.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtBind.Key)),
            ClockSkew = TimeSpan.FromMinutes(2),
        };

        // SignalR (WebSockets) cannot set Authorization header; the client
        // sends the JWT as ?access_token=... on the hub path.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole(AppRoles.SuperAdmin));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FarmerQuest Server",
        Version = "v1"
    });

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Skriv: Bearer {dit_jwt_token}"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document, null),
            new List<string>()
        }
    });
});

// Feature services
builder.Services.AddScoped<IJwtTokenFactory, JwtTokenFactory>();
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SocialService>();
builder.Services.AddScoped<PlayerStatService>();
builder.Services.AddScoped<GameSessionService>();
builder.Services.AddScoped<FarmerQuest.Server.GameFeatures.FarmSaves.FarmSaveService>();
builder.Services.AddScoped<FarmBuildingService>();
builder.Services.AddSingleton<FarmSessionAuthority>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FarmSessionAuthority>());

// Development seed templates (game data — not schema). Add more with AddSingleton<IDevDataSeeder, …>().
builder.Services.AddSingleton<IDevDataSeeder, FarmContentDevSeeder>();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations (and Dev seeders) before serving requests
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FarmerQuestDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        db.Database.Migrate();

        if (app.Environment.IsDevelopment())
        {
            var seeders = scope.ServiceProvider.GetServices<IDevDataSeeder>();
            await DevDataSeederRunner.RunAsync(db, seeders, logger);
        }
    }
    catch (SqlException ex)
    {
        // Development: allow API to start without SQL; Production: fail hard
        if (app.Environment.IsDevelopment())
        {
            logger.LogWarning(
                ex,
                "Kunne ikke forbinde til SQL Server ved startup-migrering. API starter uden automatisk migration i Development.");
        }
        else
        {
            throw new InvalidOperationException(
                $"Kunne ikke forbinde til SQL Server med ConnectionStrings:DefaultConnection. Original fejl: {ex.Message}",
                ex);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FarmerQuest Server v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameSessionHub>(GameSessionHub.Path);

app.Run();
