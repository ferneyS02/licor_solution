using Licoreria.Infrastructure.Persistence;
using Licoreria.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS (para WPF no es obligatorio, pero no estorba)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("all", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("all");

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles(); // wwwroot/imagenes
app.MapControllers();

// Migrar + Seed al arrancar
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    ctx.Database.Migrate();
    await SeedData.EnsureSeedAsync(ctx);
}

app.Run();
