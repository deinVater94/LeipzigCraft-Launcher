using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using LeipzigCraft.Launcher.Models;

namespace LeipzigCraft.Launcher.Services;

public sealed class PackService
{
    private static readonly HttpClient Http = CreateHttpClient();

    private readonly string _stateFile =
        Path.Combine(AppPaths.State, "incremental-pack-state.json");

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(45)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "LeipzigCraft-Launcher/0.2");

        return client;
    }

    public async Task<string> SyncAsync(Action<string>? status = null)
    {
        AppPaths.Ensure();

        status?.Invoke("Lade LeipzigCraft-Modliste …");

        var manifest = await DownloadManifestAsync();
        ValidateManifest(manifest);

        var state = await LoadStateAsync();
        var changedFiles = 0;

        var expectedPaths = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < manifest.Files.Count; i++)
        {
            var file = manifest.Files[i];
            var relativePath = NormalizeManagedPath(file.Path);

            expectedPaths.Add(relativePath);

            status?.Invoke(
                $"Prüfe Mod {i + 1}/{manifest.Files.Count}: " +
                Path.GetFileName(relativePath));

            if (await IsFileCurrentAsync(relativePath, file, state))
                continue;

            status?.Invoke(
                $"Aktualisiere Mod {i + 1}/{manifest.Files.Count}: " +
                Path.GetFileName(relativePath));

            await DownloadManagedFileAsync(relativePath, file, state);

            changedFiles++;
            await SaveStateAsync(state);
        }

        status?.Invoke("Räume alte Mods auf …");

        var removedFiles =
            RemoveStaleManagedMods(expectedPaths, state);

        if (manifest.ConfigArchive is not null)
        {
            var configChanged =
                await SyncConfigArchiveAsync(
                    manifest.ConfigArchive,
                    state,
                    status);

            if (configChanged)
                changedFiles++;
        }

        state.PackVersion = manifest.Version;
        state.Minecraft = manifest.Minecraft;
        state.FabricLoader = manifest.FabricLoader;

        await SaveStateAsync(state);

        if (changedFiles == 0 && removedFiles == 0)
        {
            return
                $"LeipzigCraft {manifest.Version} ist aktuell " +
                $"({manifest.Files.Count} Mods geprüft).";
        }

        return
            $"LeipzigCraft {manifest.Version} aktualisiert: " +
            $"{changedFiles} Download(s), " +
            $"{removedFiles} alte Datei(en) entfernt.";
    }

    public bool HasLocalMods()
    {
        var modsDir = Path.Combine(AppPaths.Game, "mods");

        return Directory.Exists(modsDir) &&
               Directory.EnumerateFiles(
                   modsDir,
                   "*.jar",
                   SearchOption.AllDirectories).Any();
    }

    private static async Task<PackManifest> DownloadManifestAsync()
    {
        using var response = await Http.GetAsync(
            LauncherSettings.PackManifestUrl,
            HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync();

        var manifest =
            await JsonSerializer.DeserializeAsync<PackManifest>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return manifest ??
            throw new InvalidDataException(
                "Die Online-Modliste ist leer oder ungültig.");
    }

    private static void ValidateManifest(PackManifest manifest)
    {
        if (manifest.SchemaVersion != 2)
        {
            throw new InvalidDataException(
                "Die Online-Modliste benutzt noch das alte Format. " +
                "Bitte pack.json auf SchemaVersion 2 aktualisieren.");
        }

        if (!string.Equals(
                manifest.Minecraft,
                LauncherSettings.MinecraftVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Modpack erwartet Minecraft {manifest.Minecraft}, " +
                $"Launcher ist für {LauncherSettings.MinecraftVersion} gebaut.");
        }

        if (!string.Equals(
                manifest.FabricLoader,
                LauncherSettings.FabricLoaderVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Modpack erwartet Fabric {manifest.FabricLoader}, " +
                $"Launcher ist für {LauncherSettings.FabricLoaderVersion} gebaut.");
        }

        if (manifest.Files.Count == 0)
        {
            throw new InvalidDataException(
                "Die Online-Modliste enthält keine Mod-Dateien.");
        }

        foreach (var file in manifest.Files)
        {
            _ = NormalizeManagedPath(file.Path);
            ValidateDownloadUrl(file.Url);
            ValidateSha256(file.Sha256);

            if (file.Size <= 0)
            {
                throw new InvalidDataException(
                    $"Ungültige Dateigröße für {file.Path}.");
            }
        }

        if (manifest.ConfigArchive is not null)
        {
            ValidateDownloadUrl(manifest.ConfigArchive.Url);
            ValidateSha256(manifest.ConfigArchive.Sha256);

            if (!string.Equals(
                    manifest.ConfigArchive.ExtractTo,
                    "config",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Config-Archive dürfen nur nach 'config' entpackt werden.");
            }
        }
    }

    private async Task<bool> IsFileCurrentAsync(
        string relativePath,
        PackFile remote,
        LocalPackState state)
    {
        var target = GetSafeGamePath(relativePath);

        if (!File.Exists(target))
            return false;

        var info = new FileInfo(target);

        if (info.Length != remote.Size)
            return false;

        var expectedHash = NormalizeHash(remote.Sha256);

        if (state.Files.TryGetValue(relativePath, out var cached) &&
            cached.Size == info.Length &&
            cached.LastWriteUtcTicks == info.LastWriteTimeUtc.Ticks &&
            string.Equals(
                cached.Sha256,
                expectedHash,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var localHash = await ComputeSha256Async(target);

        state.Files[relativePath] = new LocalFileState
        {
            Size = info.Length,
            LastWriteUtcTicks = info.LastWriteTimeUtc.Ticks,
            Sha256 = localHash
        };

        return string.Equals(
            localHash,
            expectedHash,
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task DownloadManagedFileAsync(
        string relativePath,
        PackFile remote,
        LocalPackState state)
    {
        ValidateDownloadUrl(remote.Url);

        var target = GetSafeGamePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        var temp = target + ".leipzigcraft-download";

        try
        {
            var actualHash =
                await DownloadAndHashAsync(
                    remote.Url,
                    temp,
                    remote.Size);

            var expectedHash = NormalizeHash(remote.Sha256);

            if (!string.Equals(
                    actualHash,
                    expectedHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"SHA256-Prüfung fehlgeschlagen: {relativePath}");
            }

            File.Move(temp, target, overwrite: true);

            var info = new FileInfo(target);

            state.Files[relativePath] = new LocalFileState
            {
                Size = info.Length,
                LastWriteUtcTicks = info.LastWriteTimeUtc.Ticks,
                Sha256 = expectedHash
            };
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private async Task<bool> SyncConfigArchiveAsync(
        PackArchive archive,
        LocalPackState state,
        Action<string>? status)
    {
        var expectedHash = NormalizeHash(archive.Sha256);

        if (string.Equals(
                state.ConfigArchiveSha256,
                expectedHash,
                StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(Path.Combine(AppPaths.Game, "config")))
        {
            return false;
        }

        status?.Invoke("Aktualisiere LeipzigCraft-Konfiguration …");

        var zipPath = Path.Combine(
            AppPaths.Cache,
            "LeipzigCraft-config.zip");

        var actualHash = await DownloadAndHashAsync(
            archive.Url,
            zipPath,
            archive.Size);

        if (!string.Equals(
                actualHash,
                expectedHash,
                StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            throw new InvalidDataException(
                "SHA256-Prüfung der Config fehlgeschlagen.");
        }

        var configDir = Path.Combine(AppPaths.Game, "config");
        Directory.CreateDirectory(configDir);

        ZipFile.ExtractToDirectory(
            zipPath,
            configDir,
            overwriteFiles: true);

        state.ConfigArchiveSha256 = expectedHash;
        return true;
    }

    private static int RemoveStaleManagedMods(
        HashSet<string> expectedPaths,
        LocalPackState state)
    {
        var modsDir = Path.Combine(AppPaths.Game, "mods");

        if (!Directory.Exists(modsDir))
            return 0;

        var removed = 0;

        foreach (var file in Directory.EnumerateFiles(
                     modsDir,
                     "*",
                     SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();

            if (extension is not ".jar" and not ".zip" and not ".rar")
                continue;

            var relative =
                Path.GetRelativePath(AppPaths.Game, file)
                    .Replace('\\', '/');

            if (expectedPaths.Contains(relative))
                continue;

            File.Delete(file);
            state.Files.Remove(relative);
            removed++;
        }

        return removed;
    }

    private static async Task<string> DownloadAndHashAsync(
        string url,
        string target,
        long expectedSize)
    {
        ValidateDownloadUrl(url);

        using var response = await Http.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var source =
            await response.Content.ReadAsStreamAsync();

        await using var destination =
            new FileStream(
                target,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                1024 * 1024,
                useAsync: true);

        using var hash =
            IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = new byte[1024 * 1024];
        long total = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer);

            if (read <= 0)
                break;

            await destination.WriteAsync(buffer.AsMemory(0, read));
            hash.AppendData(buffer, 0, read);
            total += read;
        }

        await destination.FlushAsync();

        if (expectedSize > 0 && total != expectedSize)
        {
            throw new InvalidDataException(
                $"Downloadgröße stimmt nicht: " +
                $"{total} statt {expectedSize} Bytes.");
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static async Task<string> ComputeSha256Async(string path)
    {
        await using var stream = File.OpenRead(path);

        return Convert.ToHexString(
            await SHA256.HashDataAsync(stream));
    }

    private async Task<LocalPackState> LoadStateAsync()
    {
        if (!File.Exists(_stateFile))
            return new LocalPackState();

        try
        {
            var json = await File.ReadAllTextAsync(_stateFile);

            return JsonSerializer.Deserialize<LocalPackState>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       }) ??
                   new LocalPackState();
        }
        catch
        {
            return new LocalPackState();
        }
    }

    private async Task SaveStateAsync(LocalPackState state)
    {
        Directory.CreateDirectory(AppPaths.State);

        var json = JsonSerializer.Serialize(
            state,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(_stateFile, json);
    }

    private static string NormalizeManagedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException(
                "Leerer Dateipfad in pack.json.");

        var normalized =
            path.Replace('\\', '/').TrimStart('/');

        if (!normalized.StartsWith(
                "mods/",
                StringComparison.OrdinalIgnoreCase) ||
            !normalized.EndsWith(
                ".jar",
                StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("../", StringComparison.Ordinal) ||
            normalized.Contains("/..", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Ungültiger verwalteter Mod-Pfad: {path}");
        }

        _ = GetSafeGamePath(normalized);
        return normalized;
    }

    private static string GetSafeGamePath(string relativePath)
    {
        var root =
            Path.GetFullPath(AppPaths.Game)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        var target =
            Path.GetFullPath(
                Path.Combine(
                    AppPaths.Game,
                    relativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));

        if (!target.StartsWith(
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "pack.json enthält einen unsicheren Dateipfad.");
        }

        return target;
    }

    private static void ValidateDownloadUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException(
                $"Ungültige Download-URL: {url}");
        }

        if (!string.Equals(
                uri.Host,
                "github.com",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Nicht erlaubter Download-Host: {uri.Host}");
        }

        const string requiredPrefix =
            "/deinVater94/LeipzigCraft-Launcher/releases/download/";

        if (!uri.AbsolutePath.StartsWith(
                requiredPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Download-URL gehört nicht zu einem " +
                "LeipzigCraft GitHub Release.");
        }
    }

    private static void ValidateSha256(string hash)
    {
        var normalized = NormalizeHash(hash);

        if (normalized.Length != 64 ||
            normalized.Any(c => !Uri.IsHexDigit(c)))
        {
            throw new InvalidDataException(
                "Ungültiger SHA256-Wert in pack.json.");
        }
    }

    private static string NormalizeHash(string hash)
    {
        return (hash ?? "")
            .Replace(
                "sha256:",
                "",
                StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "")
            .Trim()
            .ToUpperInvariant();
    }

    private sealed class LocalPackState
    {
        public string PackVersion { get; set; } = "";
        public string Minecraft { get; set; } = "";
        public string FabricLoader { get; set; } = "";
        public string ConfigArchiveSha256 { get; set; } = "";

        public Dictionary<string, LocalFileState> Files { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class LocalFileState
    {
        public long Size { get; set; }
        public long LastWriteUtcTicks { get; set; }
        public string Sha256 { get; set; } = "";
    }
}
