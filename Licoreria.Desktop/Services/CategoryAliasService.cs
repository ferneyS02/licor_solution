using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Licoreria.Desktop.Services;

public class CategoryAliasService
{
    private readonly string _filePath;
    private Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

    public CategoryAliasService()
    {
        _filePath = ResolvePath();
        Load();
    }

    public string GetDisplay(string realName)
    {
        var key = (realName ?? "").Trim();
        if (key.Length == 0) return "";
        return _map.TryGetValue(key, out var alias) && !string.IsNullOrWhiteSpace(alias)
            ? alias.Trim()
            : key;
    }

    public void SetAlias(string realName, string? alias)
    {
        var key = (realName ?? "").Trim();
        if (key.Length == 0) return;

        alias = (alias ?? "").Trim();

        if (string.IsNullOrWhiteSpace(alias) || alias.Equals(key, StringComparison.OrdinalIgnoreCase))
            _map.Remove(key); // si está vacío o igual al real, quitamos el alias
        else
            _map[key] = alias;

        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _map = new(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();

            _map = new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            _map = new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_map, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // si falla escritura, no rompemos el flujo
        }
    }

    private static string ResolvePath()
    {
        // 1) intenta en carpeta de la app (normalmente C:\Licoreria45\Entrega\DESKTOP\config\)
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var p1 = Path.Combine(baseDir, "config", "categorias.alias.json");
            Directory.CreateDirectory(Path.GetDirectoryName(p1)!);
            return p1;
        }
        catch
        {
            // 2) fallback: LocalAppData
            var p2 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Licoreria45", "config", "categorias.alias.json");
            return p2;
        }
    }
}
