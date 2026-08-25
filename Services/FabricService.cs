using System.IO;
using System.Net.Http;
using System.Text.Json;
using LeipzigCraft.Launcher.Models;

namespace LeipzigCraft.Launcher.Services;

public sealed class FabricService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    private static string VersionDirectory =>
        Path.Combine(AppPaths.Game, "versions", LauncherSettings.FabricVersionId);

    private static string VersionJson =>
        Path.Combine(VersionDirectory, $"{LauncherSettings.FabricVersionId}.json");

    private static string ProfileUrl =>
        $"https://meta.fabricmc.net/v2/versions/loader/" +
        $"{Uri.EscapeDataString(LauncherSettings.MinecraftVersion)}/" +
        $"{Uri.EscapeDataString(LauncherSettings.FabricLoaderVersion)}/profile/json";

    public bool IsInstalled()
    {
        return File.Exists(VersionJson) &&
               IsProfileValid(File.ReadAllText(VersionJson));
    }

    public async Task EnsureInstalledAsync(Action<string>? status = null)
    {
        AppPaths.Ensure();

        status?.Invoke($"Prüfe Fabric Loader {LauncherSettings.FabricLoaderVersion} …");

        Directory.CreateDirectory(Path.Combine(AppPaths.Game, "versions"));

        // Remove profile data created by the old fabric-installer based launcher.
        if (Directory.Exists(VersionDirectory))
            Directory.Delete(VersionDirectory, recursive: true);

        Directory.CreateDirectory(VersionDirectory);

        status?.Invoke($"Installiere Fabric Loader {LauncherSettings.FabricLoaderVersion} …");

        var json = await Http.GetStringAsync(ProfileUrl);

        if (!IsProfileValid(json))
            throw new InvalidDataException(
                "Fabric Meta hat kein gültiges Launcher-Profil geliefert.");

        await File.WriteAllTextAsync(VersionJson, json);

        status?.Invoke($"Fabric Loader {LauncherSettings.FabricLoaderVersion} ist bereit.");
    }

    private static bool IsProfileValid(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("id", out var id)) return false;
            if (!root.TryGetProperty("inheritsFrom", out var parent)) return false;
            if (!root.TryGetProperty("mainClass", out var mainClass)) return false;

            return
                id.GetString() == LauncherSettings.FabricVersionId &&
                parent.GetString() == LauncherSettings.MinecraftVersion &&
                !string.IsNullOrWhiteSpace(mainClass.GetString());
        }
        catch
        {
            return false;
        }
    }
}
