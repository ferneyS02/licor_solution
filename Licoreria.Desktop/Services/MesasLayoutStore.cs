using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Licoreria.Desktop.Services;

public class MesasLayoutStore
{
    private readonly string _path;

    private static readonly JsonSerializerOptions _opt = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public MesasLayoutStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Licoreria45");

        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "mesas_layout.json");
    }

    public MesasLayout Load()
    {
        try
        {
            if (!File.Exists(_path))
                return MesasLayout.CreateDefault();

            var json = File.ReadAllText(_path);
            var layout = JsonSerializer.Deserialize<MesasLayout>(json, _opt);

            return Normalize(layout);
        }
        catch
        {
            return MesasLayout.CreateDefault();
        }
    }

    public void Save(MesasLayout layout)
    {
        layout = Normalize(layout);
        var json = JsonSerializer.Serialize(layout, _opt);
        File.WriteAllText(_path, json);
    }

    public void Reset()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    // ✅ Garantiza compatibilidad hacia atrás y evita nulls
    private static MesasLayout Normalize(MesasLayout? layout)
    {
        var l = layout ?? MesasLayout.CreateDefault();

        if (l.Mesas == null) l.Mesas = new Dictionary<int, RectDto>();

        if (l.Barra == null)
            l.Barra = new RectDto { X = 420, Y = 520, W = 260, H = 80 };

        // ✅ BOLIRANA: caja angosta y larga (para que se vea toda la palabra)
        if (l.Bolirana == null)
            l.Bolirana = new RectDto { X = 60, Y = 220, W = 60, H = 230 };

        if (l.Tv == null)
            l.Tv = new RectDto { X = 520, Y = 40, W = 140, H = 60 };

        if (l.Afuera == null)
            l.Afuera = new RectDto { X = 900, Y = 40, W = 220, H = 60 };

        return l;
    }
}

public class MesasLayout
{
    public Dictionary<int, RectDto> Mesas { get; set; } = new();

    // ✅ Barra
    public RectDto Barra { get; set; } = new RectDto { X = 420, Y = 520, W = 260, H = 80 };

    // ✅ cajas tipo barra
    public RectDto Bolirana { get; set; } = new RectDto { X = 60, Y = 220, W = 60, H = 230 };
    public RectDto Tv { get; set; } = new RectDto { X = 520, Y = 40, W = 140, H = 60 };
    public RectDto Afuera { get; set; } = new RectDto { X = 900, Y = 40, W = 220, H = 60 };

    public static MesasLayout CreateDefault() => new MesasLayout();
}

public class RectDto
{
    public double X { get; set; }
    public double Y { get; set; }

    // Defaults para mesas (no afecta a barra/bolirana/tv/afuera porque allá se setea W/H explícito)
    public double W { get; set; } = 170;
    public double H { get; set; } = 110;
}
