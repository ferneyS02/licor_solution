using Licoreria.Domain.Entities;
using Licoreria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Infrastructure.Seed;

public static class SeedData
{
    public static async Task EnsureSeedAsync(AppDbContext ctx)
    {
        // =========================
        // ✅ USUARIOS BASE (SIN LOGIN)
        // =========================
        // Aunque no haya login, los dejamos por compatibilidad (y por Jornadas/Ordenes).
        // Importante: "sistema" debe existir porque OrdenesController lo usa.
        var changedUsers = false;

        if (!await ctx.Usuarios.AnyAsync(u => u.Nombre == "admin"))
        {
            ctx.Usuarios.Add(new Usuario
            {
                Nombre = "admin",
                Rol = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            });
            changedUsers = true;
        }

        if (!await ctx.Usuarios.AnyAsync(u => u.Nombre == "vendedor"))
        {
            ctx.Usuarios.Add(new Usuario
            {
                Nombre = "vendedor",
                Rol = "Vendedor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234")
            });
            changedUsers = true;
        }

        if (!await ctx.Usuarios.AnyAsync(u => u.Nombre == "sistema"))
        {
            ctx.Usuarios.Add(new Usuario
            {
                Nombre = "sistema",
                Rol = "Sistema",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("sistema123")
            });
            changedUsers = true;
        }

        if (changedUsers)
            await ctx.SaveChangesAsync();

        // =========================
        // ✅ MESAS FIJAS (crea las que falten)
        // =========================
        for (int i = 1; i <= 8; i++)
        {
            var nombreMesa = $"Mesa{i}";
            if (!await ctx.Mesas.AnyAsync(m => m.Nombre == nombreMesa))
            {
                ctx.Mesas.Add(new Mesa { Nombre = nombreMesa, Estado = "Disponible" });
            }
        }
        await ctx.SaveChangesAsync();

        // =========================
        // ✅ CATEGORÍAS (crea las que falten)
        // =========================
        var categorias = new[]
        {
            "Shots",
            "Cervezas",
            "Sixpacks",
            "Aguardiente / Rones",
            "Vodka",
            "Tequila",
            "Whisky",
            "Sin Alcohol",
            "Cigarrillos",
            "Snacks / Para Picar"
        };

        foreach (var c in categorias)
        {
            if (!await ctx.Categorias.AnyAsync(x => x.Nombre == c))
                ctx.Categorias.Add(new Categoria { Nombre = c });
        }
        await ctx.SaveChangesAsync();

        // Mapa de categorías para no consultar a cada rato
        var catMap = await ctx.Categorias.AsNoTracking()
            .ToDictionaryAsync(x => x.Nombre, x => x.IdCategoria);

        int Cat(string nombre) => catMap[nombre];

        // =========================
        // ✅ PRODUCTOS (no duplica)
        //    Clave: (IdCategoria + Nombre)
        // =========================
        var existentes = await ctx.Productos.AsNoTracking()
            .Select(p => new { p.IdCategoria, p.Nombre })
            .ToListAsync();

        var existsSet = new HashSet<string>(
            existentes.Select(x => $"{x.IdCategoria}::{x.Nombre}")
        );

        bool AddIfMissing(Producto p)
        {
            var key = $"{p.IdCategoria}::{p.Nombre}";
            if (existsSet.Contains(key)) return false;
            ctx.Productos.Add(p);
            existsSet.Add(key);
            return true;
        }

        // Nota: Stock ya no se usa (sin inventario). Lo dejamos en 0 para compatibilidad.
        // Si prefieres, puedes poner Stock = 999999.

        // SHOTS
        AddIfMissing(new Producto { Nombre = "Aguardiente Líder", PrecioActual = 5000, Stock = 0, Imagen = null, IdCategoria = Cat("Shots") });
        AddIfMissing(new Producto { Nombre = "Tequila José Cuervo", PrecioActual = 8000, Stock = 0, Imagen = null, IdCategoria = Cat("Shots") });
        AddIfMissing(new Producto { Nombre = "Jägermeister", PrecioActual = 10000, Stock = 0, Imagen = null, IdCategoria = Cat("Shots") });
        AddIfMissing(new Producto { Nombre = "Whisky Jack Daniels", PrecioActual = 10000, Stock = 0, Imagen = null, IdCategoria = Cat("Shots") });
        AddIfMissing(new Producto { Nombre = "Ron Boyacá", PrecioActual = 5000, Stock = 0, Imagen = null, IdCategoria = Cat("Shots") });
        AddIfMissing(new Producto { Nombre = "Vodka Absolut", PrecioActual = 8000, Stock = 0, Imagen = null, IdCategoria = Cat("Shots") });

        // CERVEZAS
        AddIfMissing(new Producto { Nombre = "Águila", PrecioActual = 3000, Stock = 0, Imagen = "aguila.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Águila Light", PrecioActual = 3500, Stock = 0, Imagen = "aguila_light.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Águila Lata", PrecioActual = 4000, Stock = 0, Imagen = "aguila_lata.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Poker", PrecioActual = 3000, Stock = 0, Imagen = "poker.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Poker Lata", PrecioActual = 4000, Stock = 0, Imagen = "poker_lata.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Costeña", PrecioActual = 3000, Stock = 0, Imagen = "costena.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Costeña Bacana", PrecioActual = 3000, Stock = 0, Imagen = "costena_bacana.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Costeña Bacana Lata", PrecioActual = 4000, Stock = 0, Imagen = "costena_bacana_lata.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Club Colombia Lata", PrecioActual = 4500, Stock = 0, Imagen = "club_colombia_lata.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Corona", PrecioActual = 6000, Stock = 0, Imagen = "corona.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Coronita", PrecioActual = 4500, Stock = 0, Imagen = "coronita.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Budweiser Lata", PrecioActual = 3000, Stock = 0, Imagen = "budweiser_lata.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Four Loko", PrecioActual = 20000, Stock = 0, Imagen = "four_loko.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Redds", PrecioActual = 4500, Stock = 0, Imagen = "redds.png", IdCategoria = Cat("Cervezas") });
        AddIfMissing(new Producto { Nombre = "Cuates", PrecioActual = 4500, Stock = 0, Imagen = "cuates.png", IdCategoria = Cat("Cervezas") });

        // SIXPACKS
        AddIfMissing(new Producto { Nombre = "Six Águila", PrecioActual = 24000, Stock = 0, Imagen = "six_aguila.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Poker", PrecioActual = 24000, Stock = 0, Imagen = "six_poker.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Costeña Bacana", PrecioActual = 24000, Stock = 0, Imagen = "six_costena_bacana.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Club Colombia", PrecioActual = 27000, Stock = 0, Imagen = "six_club_colombia.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Coronita", PrecioActual = 27000, Stock = 0, Imagen = "six_coronita.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Corona", PrecioActual = 36000, Stock = 0, Imagen = "six_corona.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Redds", PrecioActual = 27000, Stock = 0, Imagen = "six_redds.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Costeña", PrecioActual = 18000, Stock = 0, Imagen = "six_costena.png", IdCategoria = Cat("Sixpacks") });
        AddIfMissing(new Producto { Nombre = "Six Budweiser", PrecioActual = 18000, Stock = 0, Imagen = "six_budweiser.png", IdCategoria = Cat("Sixpacks") });

        // AGUARDIENTE / RONES
        AddIfMissing(new Producto { Nombre = "Líder Media", PrecioActual = 37000, Stock = 0, Imagen = "lider_media.png", IdCategoria = Cat("Aguardiente / Rones") });
        AddIfMissing(new Producto { Nombre = "Líder Botella / Onix / Onix Amarillo", PrecioActual = 63000, Stock = 0, Imagen = "lider_botella.png", IdCategoria = Cat("Aguardiente / Rones") });
        AddIfMissing(new Producto { Nombre = "Aguardiente Amarillo de Manzanares", PrecioActual = 65000, Stock = 0, Imagen = "amarillo_manzanares.png", IdCategoria = Cat("Aguardiente / Rones") });
        AddIfMissing(new Producto { Nombre = "Boyacá Media", PrecioActual = 39000, Stock = 0, Imagen = "boyaca_media.png", IdCategoria = Cat("Aguardiente / Rones") });
        AddIfMissing(new Producto { Nombre = "Boyacá Botella", PrecioActual = 65000, Stock = 0, Imagen = "boyaca_botella.png", IdCategoria = Cat("Aguardiente / Rones") });
        AddIfMissing(new Producto { Nombre = "Caldas Media", PrecioActual = 41000, Stock = 0, Imagen = "caldas_media.png", IdCategoria = Cat("Aguardiente / Rones") });
        AddIfMissing(new Producto { Nombre = "Caldas Botella", PrecioActual = 70000, Stock = 0, Imagen = "caldas_botella.png", IdCategoria = Cat("Aguardiente / Rones") });
        AddIfMissing(new Producto { Nombre = "Bacardí", PrecioActual = 68000, Stock = 0, Imagen = "bacardi.png", IdCategoria = Cat("Aguardiente / Rones") });

        // VODKA
        AddIfMissing(new Producto { Nombre = "Absolut Botella", PrecioActual = 110000, Stock = 0, Imagen = "absolut_botella.png", IdCategoria = Cat("Vodka") });
        AddIfMissing(new Producto { Nombre = "Smirnoff Lulo Media", PrecioActual = 35000, Stock = 0, Imagen = "smirnoff_lulo_media.png", IdCategoria = Cat("Vodka") });
        AddIfMissing(new Producto { Nombre = "Smirnoff Lulo Botella", PrecioActual = 60000, Stock = 0, Imagen = "smirnoff_lulo_botella.png", IdCategoria = Cat("Vodka") });

        // TEQUILA
        AddIfMissing(new Producto { Nombre = "Olmeca", PrecioActual = 100000, Stock = 0, Imagen = "olmeca.png", IdCategoria = Cat("Tequila") });

        // WHISKY
        AddIfMissing(new Producto { Nombre = "Black & White Media", PrecioActual = 40000, Stock = 0, Imagen = "black_white_media.png", IdCategoria = Cat("Whisky") });
        AddIfMissing(new Producto { Nombre = "Black & White Botella", PrecioActual = 70000, Stock = 0, Imagen = "black_white_botella.png", IdCategoria = Cat("Whisky") });
        AddIfMissing(new Producto { Nombre = "Red Label Botella", PrecioActual = 90000, Stock = 0, Imagen = "red_label.png", IdCategoria = Cat("Whisky") });
        AddIfMissing(new Producto { Nombre = "Something Special Botella", PrecioActual = 90000, Stock = 0, Imagen = "something_special.png", IdCategoria = Cat("Whisky") });
        AddIfMissing(new Producto { Nombre = "Buchanan's Deluxe Botella", PrecioActual = 190000, Stock = 0, Imagen = "buchanans_deluxe.png", IdCategoria = Cat("Whisky") });
        AddIfMissing(new Producto { Nombre = "Jack Daniels ", PrecioActual = 190000, Stock = 0, Imagen = "jack_miel.png", IdCategoria = Cat("Whisky") });
        AddIfMissing(new Producto { Nombre = "Chivas Botella", PrecioActual = 190000, Stock = 0, Imagen = "chivas.png", IdCategoria = Cat("Whisky") });
        AddIfMissing(new Producto { Nombre = "Jägermeister Botella", PrecioActual = 185000, Stock = 0, Imagen = "jagermeister_botella.png", IdCategoria = Cat("Whisky") });

        // SIN ALCOHOL
        AddIfMissing(new Producto { Nombre = "Gatorade", PrecioActual = 5000, Stock = 0, Imagen = "gatorade.png", IdCategoria = Cat("Sin Alcohol") });
        AddIfMissing(new Producto { Nombre = "Coca-Cola", PrecioActual = 5000, Stock = 0, Imagen = "cocacola.png", IdCategoria = Cat("Sin Alcohol") });
        AddIfMissing(new Producto { Nombre = "Agua", PrecioActual = 2500, Stock = 0, Imagen = "agua.png", IdCategoria = Cat("Sin Alcohol") });
        AddIfMissing(new Producto { Nombre = "Bonfiest", PrecioActual = 5000, Stock = 0, Imagen = "bonfiest.png", IdCategoria = Cat("Sin Alcohol") });

        // CIGARRILLOS
        AddIfMissing(new Producto { Nombre = "Lucky Medio", PrecioActual = 8000, Stock = 0, Imagen = "lucky_medio.png", IdCategoria = Cat("Cigarrillos") });
        AddIfMissing(new Producto { Nombre = "Marlboro Medio", PrecioActual = 8000, Stock = 0, Imagen = "marlboro_medio.png", IdCategoria = Cat("Cigarrillos") });
        AddIfMissing(new Producto { Nombre = "Rothman Blanco Medio", PrecioActual = 6000, Stock = 0, Imagen = "rothman_blanco_medio.png", IdCategoria = Cat("Cigarrillos") });
        AddIfMissing(new Producto { Nombre = "Rothman Medio", PrecioActual = 7000, Stock = 0, Imagen = "rothman_medio.png", IdCategoria = Cat("Cigarrillos") });
        AddIfMissing(new Producto { Nombre = "Cigarrillo Unidad", PrecioActual = 1500, Stock = 0, Imagen = "cigarrillo_unidad.png", IdCategoria = Cat("Cigarrillos") });
        AddIfMissing(new Producto { Nombre = "Encendedor", PrecioActual = 3000, Stock = 0, Imagen = "encendedor.png", IdCategoria = Cat("Cigarrillos") });

        // SNACKS / PARA PICAR
        AddIfMissing(new Producto { Nombre = "Detodito Grande", PrecioActual = 9000, Stock = 0, Imagen = "detodito_grande.png", IdCategoria = Cat("Snacks / Para Picar") });
        AddIfMissing(new Producto { Nombre = "Detodito Pequeño", PrecioActual = 4000, Stock = 0, Imagen = "detodito_pequeno.png", IdCategoria = Cat("Snacks / Para Picar") });
        AddIfMissing(new Producto { Nombre = "Bombombum", PrecioActual = 1000, Stock = 0, Imagen = "bombombum.png", IdCategoria = Cat("Snacks / Para Picar") });
        AddIfMissing(new Producto { Nombre = "Trident", PrecioActual = 1000, Stock = 0, Imagen = "trident.png", IdCategoria = Cat("Snacks / Para Picar") });

        await ctx.SaveChangesAsync();
    }
}
