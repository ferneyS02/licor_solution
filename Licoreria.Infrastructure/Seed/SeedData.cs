using Licoreria.Domain.Entities;
using Licoreria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Infrastructure.Seed;

public static class SeedData
{
    public static async Task EnsureSeedAsync(AppDbContext ctx)
    {
        // Mesas fijas
        if (!await ctx.Mesas.AnyAsync())
        {
            ctx.Mesas.AddRange(
                new Mesa { Nombre = "Mesa1" },
                new Mesa { Nombre = "Mesa2" },
                new Mesa { Nombre = "Mesa3" },
                new Mesa { Nombre = "Mesa4" },
                new Mesa { Nombre = "Mesa5" },
                new Mesa { Nombre = "Mesa6" },
                new Mesa { Nombre = "Mesa7" },
                new Mesa { Nombre = "Mesa8" }
            );
            await ctx.SaveChangesAsync();
        }

        // Categorías
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

        int Cat(string nombre) => ctx.Categorias.First(x => x.Nombre == nombre).IdCategoria;

        // Productos (si ya hay, no duplica)
        if (await ctx.Productos.AnyAsync()) return;

        var list = new List<Producto>();

        // SHOTS :contentReference[oaicite:6]{index=6}
        list.AddRange(new[]
        {
            new Producto { Nombre="Aguardiente Líder", PrecioActual=5000, Stock=0, Imagen=null, IdCategoria=Cat("Shots") },
            new Producto { Nombre="Tequila José Cuervo", PrecioActual=8000, Stock=0, Imagen=null, IdCategoria=Cat("Shots") },
            new Producto { Nombre="Jägermeister", PrecioActual=10000, Stock=0, Imagen=null, IdCategoria=Cat("Shots") },
            new Producto { Nombre="Whisky Jack Daniels", PrecioActual=10000, Stock=0, Imagen=null, IdCategoria=Cat("Shots") },
            new Producto { Nombre="Ron Boyacá", PrecioActual=5000, Stock=0, Imagen=null, IdCategoria=Cat("Shots") },
            new Producto { Nombre="Vodka Absolut", PrecioActual=8000, Stock=0, Imagen=null, IdCategoria=Cat("Shots") },
        });

        // CERVEZAS :contentReference[oaicite:7]{index=7}
        list.AddRange(new[]
        {
            new Producto { Nombre="Águila", PrecioActual=3000, Stock=0, Imagen="aguila.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Águila Light", PrecioActual=3500, Stock=0, Imagen="aguila_light.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Águila Lata", PrecioActual=4000, Stock=0, Imagen="aguila_lata.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Poker", PrecioActual=3000, Stock=0, Imagen="poker.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Poker Lata", PrecioActual=4000, Stock=0, Imagen="poker_lata.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Costeña", PrecioActual=3000, Stock=0, Imagen="costena.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Costeña Bacana", PrecioActual=3000, Stock=0, Imagen="costena_bacana.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Costeña Bacana Lata", PrecioActual=4000, Stock=0, Imagen="costena_bacana_lata.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Club Colombia Lata", PrecioActual=4500, Stock=0, Imagen="club_colombia_lata.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Corona", PrecioActual=6000, Stock=0, Imagen="corona.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Coronita", PrecioActual=4500, Stock=0, Imagen="coronita.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Budweiser Lata", PrecioActual=3000, Stock=0, Imagen="budweiser_lata.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Four Loko", PrecioActual=20000, Stock=0, Imagen="four_loko.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Redds", PrecioActual=4500, Stock=0, Imagen="redds.png", IdCategoria=Cat("Cervezas") },
            new Producto { Nombre="Cuates", PrecioActual=4500, Stock=0, Imagen="cuates.png", IdCategoria=Cat("Cervezas") },
        });

        // SIXPACKS :contentReference[oaicite:8]{index=8} :contentReference[oaicite:9]{index=9}
        list.AddRange(new[]
        {
            new Producto { Nombre="Six Águila", PrecioActual=24000, Stock=0, Imagen="six_aguila.png", IdCategoria=Cat("Sixpacks") },
            new Producto { Nombre="Six Poker", PrecioActual=24000, Stock=0, Imagen="six_poker.png", IdCategoria=Cat("Sixpacks") },
            new Producto { Nombre="Six Costeña Bacana", PrecioActual=24000, Stock=0, Imagen="six_costena_bacana.png", IdCategoria=Cat("Sixpacks") },
            new Producto { Nombre="Six Club Colombia", PrecioActual=27000, Stock=0, Imagen="six_club_colombia.png", IdCategoria=Cat("Sixpacks") },
            new Producto { Nombre="Six Coronita", PrecioActual=27000, Stock=0, Imagen="six_coronita.png", IdCategoria=Cat("Sixpacks") },
            new Producto { Nombre="Six Corona", PrecioActual=36000, Stock=0, Imagen="six_corona.png", IdCategoria=Cat("Sixpacks") },
            new Producto { Nombre="Six Redds", PrecioActual=27000, Stock=0, Imagen="six_redds.png", IdCategoria=Cat("Sixpacks") },

            // En el PDF viene "Costeña / Budweiser: 18.000" :contentReference[oaicite:10]{index=10}
            // Los separamos (como tú lo querías)
            new Producto { Nombre="Six Costeña", PrecioActual=18000, Stock=0, Imagen="six_costena.png", IdCategoria=Cat("Sixpacks") },
            new Producto { Nombre="Six Budweiser", PrecioActual=18000, Stock=0, Imagen="six_budweiser.png", IdCategoria=Cat("Sixpacks") },
        });

        // AGUARDIENTE / RONES :contentReference[oaicite:11]{index=11}
        list.AddRange(new[]
        {
            new Producto { Nombre="Líder Media", PrecioActual=37000, Stock=0, Imagen="lider_media.png", IdCategoria=Cat("Aguardiente / Rones") },
            new Producto { Nombre="Líder Botella / Onix / Onix Amarillo", PrecioActual=63000, Stock=0, Imagen="lider_botella.png", IdCategoria=Cat("Aguardiente / Rones") },
            new Producto { Nombre="Aguardiente Amarillo de Manzanares", PrecioActual=65000, Stock=0, Imagen="amarillo_manzanares.png", IdCategoria=Cat("Aguardiente / Rones") },
            new Producto { Nombre="Boyacá Media", PrecioActual=39000, Stock=0, Imagen="boyaca_media.png", IdCategoria=Cat("Aguardiente / Rones") },
            new Producto { Nombre="Boyacá Botella", PrecioActual=65000, Stock=0, Imagen="boyaca_botella.png", IdCategoria=Cat("Aguardiente / Rones") },
            new Producto { Nombre="Caldas Media", PrecioActual=41000, Stock=0, Imagen="caldas_media.png", IdCategoria=Cat("Aguardiente / Rones") },
            new Producto { Nombre="Caldas Botella", PrecioActual=70000, Stock=0, Imagen="caldas_botella.png", IdCategoria=Cat("Aguardiente / Rones") },
            new Producto { Nombre="Bacardí", PrecioActual=68000, Stock=0, Imagen="bacardi.png", IdCategoria=Cat("Aguardiente / Rones") },
        });

        // VODKA :contentReference[oaicite:12]{index=12}
        list.AddRange(new[]
        {
            new Producto { Nombre="Absolut Botella", PrecioActual=110000, Stock=0, Imagen="absolut_botella.png", IdCategoria=Cat("Vodka") },
            new Producto { Nombre="Smirnoff Lulo Media", PrecioActual=35000, Stock=0, Imagen="smirnoff_lulo_media.png", IdCategoria=Cat("Vodka") },
            new Producto { Nombre="Smirnoff Lulo Botella", PrecioActual=60000, Stock=0, Imagen="smirnoff_lulo_botella.png", IdCategoria=Cat("Vodka") },
        });

        // TEQUILA :contentReference[oaicite:13]{index=13}
        list.Add(new Producto { Nombre = "Olmeca", PrecioActual = 100000, Stock = 0, Imagen = "olmeca.png", IdCategoria = Cat("Tequila") });

        // WHISKY :contentReference[oaicite:14]{index=14}
        list.AddRange(new[]
        {
            new Producto { Nombre="Black & White Media", PrecioActual=40000, Stock=0, Imagen="black_white_media.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Black & White Botella", PrecioActual=70000, Stock=0, Imagen="black_white_botella.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Red Label Botella", PrecioActual=90000, Stock=0, Imagen="red_label.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Something Special Botella", PrecioActual=90000, Stock=0, Imagen="something_special.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Jack Daniel’s No.7 Tennessee Whiskey", PrecioActual=90000, Stock=0, Imagen="jack_no7.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Buchanan’s Deluxe Botella", PrecioActual=190000, Stock=0, Imagen="buchanans_deluxe.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Jack Daniel’s Miel Botella", PrecioActual=190000, Stock=0, Imagen="jack_miel.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Chivas Botella", PrecioActual=190000, Stock=0, Imagen="chivas.png", IdCategoria=Cat("Whisky") },
            new Producto { Nombre="Jägermeister", PrecioActual=185000, Stock=0, Imagen="jagermeister_botella.png", IdCategoria=Cat("Whisky") },
        });

        // SIN ALCOHOL :contentReference[oaicite:15]{index=15}
        list.AddRange(new[]
        {
            new Producto { Nombre="Gatorade", PrecioActual=5000, Stock=0, Imagen="gatorade.png", IdCategoria=Cat("Sin Alcohol") },
            new Producto { Nombre="Coca-Cola", PrecioActual=5000, Stock=0, Imagen="cocacola.png", IdCategoria=Cat("Sin Alcohol") },
            new Producto { Nombre="Agua", PrecioActual=2500, Stock=0, Imagen="agua.png", IdCategoria=Cat("Sin Alcohol") },
            new Producto { Nombre="Bonfiest", PrecioActual=5000, Stock=0, Imagen="bonfiest.png", IdCategoria=Cat("Sin Alcohol") },
        });

        // CIGARRILLOS :contentReference[oaicite:16]{index=16}
        list.AddRange(new[]
        {
            new Producto { Nombre="Lucky Medio", PrecioActual=8000, Stock=0, Imagen="lucky_medio.png", IdCategoria=Cat("Cigarrillos") },
            new Producto { Nombre="Marlboro Medio", PrecioActual=8000, Stock=0, Imagen="marlboro_medio.png", IdCategoria=Cat("Cigarrillos") },
            new Producto { Nombre="Rothman Blanco Medio", PrecioActual=6000, Stock=0, Imagen="rothman_blanco_medio.png", IdCategoria=Cat("Cigarrillos") },
            new Producto { Nombre="Rothman Medio", PrecioActual=7000, Stock=0, Imagen="rothman_medio.png", IdCategoria=Cat("Cigarrillos") },
            new Producto { Nombre="Cigarrillo Unidad", PrecioActual=1500, Stock=0, Imagen="cigarrillo_unidad.png", IdCategoria=Cat("Cigarrillos") },
            new Producto { Nombre="Encendedor", PrecioActual=3000, Stock=0, Imagen="encendedor.png", IdCategoria=Cat("Cigarrillos") },
        });

        // SNACKS / PARA PICAR :contentReference[oaicite:17]{index=17}
        list.AddRange(new[]
        {
            new Producto { Nombre="Detodito Grande", PrecioActual=9000, Stock=0, Imagen="detodito_grande.png", IdCategoria=Cat("Snacks / Para Picar") },
            new Producto { Nombre="Detodito Pequeño", PrecioActual=4000, Stock=0, Imagen="detodito_pequeno.png", IdCategoria=Cat("Snacks / Para Picar") },
            new Producto { Nombre="Bombombum", PrecioActual=1000, Stock=0, Imagen="bombombum.png", IdCategoria=Cat("Snacks / Para Picar") },
            new Producto { Nombre="Trident", PrecioActual=1000, Stock=0, Imagen="trident.png", IdCategoria=Cat("Snacks / Para Picar") },
        });

        ctx.Productos.AddRange(list);
        await ctx.SaveChangesAsync();
    }
}
