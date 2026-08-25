using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using LeipzigCraft.Launcher.Models;

namespace LeipzigCraft.Launcher.Services;

public sealed class PackService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(45)
    };

    private readonly string _installedVersionFile =
        Path.Combine(AppPaths.State, "pack-version.txt");

    public async Task<string> SyncAsync(Action<string>? status = null)
    {
        AppPaths.Ensure();

        PackManifest? manifest;

        try
        {
            status?.Invoke("Prüfe LeipzigCraft-Modpack …");

            using var response = await Http.GetAsync(
                LauncherSettings.PackManifestUrl,
                HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return "Remote-Pack noch nicht veröffentlicht.";
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            manifest = await JsonSerializer.DeserializeAsync<PackManifest>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            return $"Pack-Prüfung übersprungen: {ex.Message}";
        }

        if (manifest is null ||
            string.IsNullOrWhiteSpace(manifest.Version) ||
            string.IsNullOrWhiteSpace(manifest.ZipUrl))
        {
            return "Remote-Pack noch nicht konfiguriert.";
        }

        var installedVersion =
            File.Exists(_installedVersionFile)
                ? (await File.ReadAllTextAsync(_installedVersionFile)).Trim()
                : "";

        var modsDir = Path.Combine(AppPaths.Game, "mods");

        if (installedVersion == manifest.Version &&
            Directory.Exists(modsDir) &&
            Directory.EnumerateFiles(modsDir, "*.jar").Any())
        {
            return $"Modpack {manifest.Version} ist aktuell.";
        }

        status?.Invoke($"Lade Modpack {manifest.Version} …");

        var zipPath = Path.Combine(
            AppPaths.Cache,
            $"LeipzigCraft-Pack-{SafeFileName(manifest.Version)}.zip");

        await DownloadAsync(manifest.ZipUrl, zipPath);

        if (!string.IsNullOrWhiteSpace(manifest.Sha256))
        {
            status?.Invoke("Prüfe Modpack-Integrität …");
            await VerifySha256Async(zipPath, manifest.Sha256);
        }

        status?.Invoke("Installiere Modpack …");

        if (Directory.Exists(modsDir))
            Directory.Delete(modsDir, recursive: true);

        Directory.CreateDirectory(modsDir);

        ZipFile.ExtractToDirectory(
            zipPath,
            AppPaths.Game,
            overwriteFiles: true);

        await File.WriteAllTextAsync(
            _installedVersionFile,
            manifest.Version);

        return $"Modpack {manifest.Version} installiert.";
    }

    public bool HasLocalMods()
    {
        var modsDir = Path.Combine(AppPaths.Game, "mods");

        return Directory.Exists(modsDir) &&
               Directory.EnumerateFiles(modsDir, "*.jar").Any();
    }

    private static async Task DownloadAsync(string url, string target)
    {
        using var response = await Http.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var destination = new FileStream(
            target,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            1024 * 1024,
            useAsync: true);

        await source.CopyToAsync(destination);
    }

    private static async Task VerifySha256Async(
        string path,
        string expectedHash)
    {
        await using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(
            await SHA256.HashDataAsync(stream));

        var expected = expectedHash
            .Replace("sha256:", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "")
            .Trim()
            .ToUpperInvariant();

        if (!string.Equals(
                actual,
                expected,
                StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(path);

            throw new InvalidDataException(
                "SHA256-Prüfung des Modpacks fehlgeschlagen.");
        }
    }

    private static string SafeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value;
    }
}
