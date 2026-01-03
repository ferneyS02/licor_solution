using Licoreria.Infrastructure.Persistence;
using Licoreria.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF: selecciona licencia (OBLIGATORIO)
QuestPDF.Settings.License = LicenseType.Community;

// (opcional) fuerza el puerto fijo
builder.WebHost.UseUrls("http://localhost:5128");

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Licoreria API", Version = "v1" });

    // ?? Swagger: Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el token JWT. Ej: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ?? JWT Auth
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Falta Jwt:Key en appsettings.json");
var jwtIssuer = jwt["Issuer"] ?? "Licoreria45";
var jwtAudience = jwt["Audience"] ?? "Licoreria45Desktop";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("all", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("all");

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// health
app.MapGet("/api/health", () => Results.Ok(new { ok = true }));

app.MapControllers();

// Migraciones + Seed
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    ctx.Database.Migrate();
    await SeedData.EnsureSeedAsync(ctx);
}

app.Run();
