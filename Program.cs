using System.Security.Claims;
using GestionProduits.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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

    // -- Keycloak JWT via JWKS ----------------------------------------------------
    var keycloakSection = builder.Configuration.GetSection("Keycloak");
    var realm = keycloakSection["Realm"]!;
    // URL interne : sert à récupérer metadata + JWKS (doit être joignable par l'API,
    // ex. http://keycloak:8080 en Docker).
    var authServerUrl = keycloakSection["AuthServerUrl"]!;
    var issuer = $"{authServerUrl}/realms/{realm}";
    var metadataAddress = $"{issuer}/.well-known/openid-configuration";
    var allowHttpMetadata = builder.Environment.IsDevelopment();

    // Issuer externe : celui qui figure dans le claim "iss" des tokens, tel que vu
    // par le client qui les obtient (ex. http://localhost:8180). En Docker il diffère
    // de l'URL interne, donc on valide contre les deux.
    var publicAuthServerUrl = keycloakSection["ValidIssuer"];
    var validIssuers = string.IsNullOrWhiteSpace(publicAuthServerUrl)
        ? new[] { issuer }
        : new[] { issuer, $"{publicAuthServerUrl}/realms/{realm}" }.Distinct().ToArray();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.RequireHttpsMetadata = !allowHttpMetadata;
            opt.MetadataAddress = metadataAddress;
            opt.Authority = allowHttpMetadata ? null : issuer;
            opt.Audience = keycloakSection["ClientId"];

            if (allowHttpMetadata)
            {
                opt.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever { RequireHttps = false });
            }

            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = validIssuers,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "preferred_username",
                RoleClaimType = ClaimTypes.Role
            };

            opt.Events = new JwtBearerEvents
            {
                OnTokenValidated = ctx =>
                {
                    var identity = ctx.Principal?.Identity as ClaimsIdentity;
                    if (identity is null) return Task.CompletedTask;

                    var realmAccess = ctx.Principal?
                        .FindFirst("realm_access")?.Value;
                    if (realmAccess is not null)
                    {
                        var roles = System.Text.Json.JsonDocument
                            .Parse(realmAccess)
                            .RootElement
                            .GetProperty("roles")
                            .EnumerateArray()
                            .Select(r => r.GetString()!);

                        foreach (var role in roles)
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
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

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
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
