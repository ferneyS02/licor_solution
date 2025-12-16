using Licoreria.Infrastructure.Persistence;
using Licoreria.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (por si luego haces app móvil o web)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("all", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

// Archivos estáticos (wwwroot/imagenes)
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

app.UseCors("all");

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();  // sirve /imagenes/<archivo>
app.UseRouting();

app.MapControllers();

// Migraciones + seed al arrancar
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    ctx.Database.Migrate();
    await SeedData.EnsureSeedAsync(ctx);
}

app.Run();
