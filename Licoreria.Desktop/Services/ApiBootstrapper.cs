using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace Licoreria.Desktop.Services;

public static class ApiBootstrapper
{
    private static Process? _apiProcess;

    public static async Task<bool> EnsureApiRunningAsync()
    {
        if (await IsApiUpAsync()) return true;

        TryStartApi();

        for (int i = 0; i < 12; i++)
        {
            await Task.Delay(250);
            if (await IsApiUpAsync()) return true;
        }

        return false;
    }

    public static void TryStopApi()
    {
        try
        {
            if (_apiProcess is { HasExited: false })
                _apiProcess.Kill(entireProcessTree: true);
        }
        catch { }
    }

    private static async Task<bool> IsApiUpAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(700) };
            var url = $"{ApiConfig.API.TrimEnd('/')}/health"; // /api/health
            using var res = await http.GetAsync(url);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static void TryStartApi()
    {
        try
        {
            var desktopDir = AppDomain.CurrentDomain.BaseDirectory;
            var root = FindSolutionRoot(desktopDir);

            if (root != null)
            {
                var csproj = Path.Combine(root, "Licoreria.Api", "Licoreria.Api.csproj");
                if (File.Exists(csproj))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project \"{csproj}\" --urls \"http://localhost:5128\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = root
                    };
                    psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

                    _apiProcess = Process.Start(psi);
                }
            }
        }
        catch { }
    }

    private static string? FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            var apiFolder = Path.Combine(dir.FullName, "Licoreria.Api");
            var desktopFolder = Path.Combine(dir.FullName, "Licoreria.Desktop");

            if (Directory.Exists(apiFolder) && Directory.Exists(desktopFolder))
                return dir.FullName;

            dir = dir.Parent;
        }
        return null;
    }
}
