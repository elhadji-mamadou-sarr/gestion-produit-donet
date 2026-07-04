// // Program.cs
// using GestionProduits.Api.Data;
// using GestionProduits.Api.Services;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.IdentityModel.Tokens;
// using Microsoft.OpenApi.Models;
// using Serilog;
// using System.Text;

// // ── Serilog ───────────────────────────────────────────────────────────────
// Log.Logger = new LoggerConfiguration()
//     .WriteTo.Console()
//     .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
//     .CreateLogger();

// var builder = WebApplication.CreateBuilder(args);
// builder.Host.UseSerilog();

// // ── Services de base ──────────────────────────────────────────────────────
// builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddScoped<ITokenService, TokenService>();

// // ── Base de données PostgreSQL ────────────────────────────────────────────
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// // ── CORS (Angular) ────────────────────────────────────────────────────────
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAngular", policy =>
//         policy.WithOrigins("http://localhost:4200")
//               .AllowAnyHeader()
//               .AllowAnyMethod());
// });

// // ── JWT Authentication ────────────────────────────────────────────────────
// var jwtSettings = builder.Configuration.GetSection("JwtSettings");
// var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
// })
// .AddJwtBearer(options =>
// {
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         ValidateIssuer = true,
//         ValidateAudience = true,
//         ValidateLifetime = true,
//         ValidateIssuerSigningKey = true,
//         ValidIssuer = jwtSettings["Issuer"],
//         ValidAudience = jwtSettings["Audience"],
//         IssuerSigningKey = new SymmetricSecurityKey(key),
//         ClockSkew = TimeSpan.Zero
//     };
// });

// // ── Swagger avec support JWT ──────────────────────────────────────────────
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo
//     {
//         Title = "GestionProduits API",
//         Version = "v1"
//     });

//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Name = "Authorization",
//         Type = SecuritySchemeType.Http,
//         Scheme = "Bearer",
//         BearerFormat = "JWT",
//         In = ParameterLocation.Header,
//         Description = "Entrez votre token JWT : Bearer {token}"
//     });

//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type = ReferenceType.SecurityScheme,
//                     Id   = "Bearer"
//                 }
//             },
//             Array.Empty<string>()
//         }
//     });
// });

// // ═══════════════════════════════════════════════════════════════════════════
// var app = builder.Build();
// // ═══════════════════════════════════════════════════════════════════════════

// app.UseSwagger();
// app.UseSwaggerUI(c =>
// {
//     c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionProduits API v1");
// });

// // ── Middleware (ordre important !) ─────────────────────────────────────────
// app.UseCors("AllowAngular");
// app.UseAuthentication(); // ← avant UseAuthorization
// app.UseAuthorization();

// app.MapControllers();
// app.Run();


// Program.cs
using GestionProduits.Api.Data;
using GestionProduits.Api.Dtos;
using GestionProduits.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;                          // ← ajoute
using Serilog;
using System.Text;

// ── Serilog ───────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddMetrics();             // ← ajoute

// ── PostgreSQL ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── JWT ───────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(key),
        ClockSkew                = TimeSpan.Zero
    };
});

// ── Swagger ───────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GestionProduits API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Entrez votre token JWT"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ═════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ═════════════════════════════════════════════════════════════════════════

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionProduits API v1");
});

app.UseCors("AllowAngular");
app.UseHttpMetrics();                      // ← ajoute (latence, codes HTTP)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMetrics();                          // ← expose /metrics

app.Run();