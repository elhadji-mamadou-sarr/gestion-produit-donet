using System.Text;
using GestionProduits.Api.Infrastructure;
using GestionProduits.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Compact;


Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter())
        .WriteTo.File("logs/app-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection est manquant.")));

    var jwtSection = builder.Configuration.GetSection("Jwt");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddControllers();

    builder.Services.AddCors(o => o.AddPolicy("AllowAngular", p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "GestionProduits API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });
        // Sécurité Swagger (optionnel) : selon la version Swashbuckle/Microsoft.OpenApi,
        // les types OpenApiSecuritySchemeReference/OpenApiReference peuvent ne pas exister.
        // On laisse Swagger sans la contrainte de schéma (la spec reste valide).
        // c.AddSecurityRequirement(...);

    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

    app.UseSerilogRequestLogging(opts =>
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)");

    if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

    app.UseHttpsRedirection();
    app.UseCors("AllowAngular");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex) { Log.Fatal(ex, "Application terminée de façon inattendue"); }
finally { Log.CloseAndFlush(); }

// using GestionProduits.Api.Data;
// using Microsoft.EntityFrameworkCore;
// using Serilog;

// Log.Logger = new LoggerConfiguration()
//     .WriteTo.Console()
//     .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
//     .CreateLogger();

// var builder = WebApplication.CreateBuilder(args);
// builder.Host.UseSerilog();

// builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAngular", policy =>
//         policy.WithOrigins("http://localhost:4200")
//               .AllowAnyHeader()
//               .AllowAnyMethod());
// });

// var app = builder.Build();

// app.UseSwagger();
// app.UseSwaggerUI();
// app.UseCors("AllowAngular");
// app.UseAuthorization();
// app.MapControllers();
// app.Run();
