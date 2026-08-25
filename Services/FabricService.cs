using System.Diagnostics;
using System.Net.Http;
using System.Xml.Linq;
using LeipzigCraft.Launcher.Models;

namespace LeipzigCraft.Launcher.Services;

public sealed class FabricService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public bool IsInstalled()
    {
        var json = Path.Combine(
            AppPaths.Game,
            "versions",
            LauncherSettings.FabricVersionId,
            $"{LauncherSettings.FabricVersionId}.json");

        return File.Exists(json);
    }

    public async Task EnsureInstalledAsync(Action<string>? status = null)
    {
        if (IsInstalled())
            return;

        status?.Invoke("Installiere Fabric Loader 0.18.4 …");

        var java = FindJava();

        if (java is null)
        {
            throw new FileNotFoundException(
                "Java wurde noch nicht gefunden. " +
                "Minecraft 1.21 muss zuerst installiert werden.");
        }

        var installer = await GetFabricInstallerAsync(status);

        var psi = new ProcessStartInfo
        {
            FileName = java,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("-jar");
        psi.ArgumentList.Add(installer);
        psi.ArgumentList.Add("client");
        psi.ArgumentList.Add("-dir");
        psi.ArgumentList.Add(AppPaths.Game);
        psi.ArgumentList.Add("-mcversion");
        psi.ArgumentList.Add(LauncherSettings.MinecraftVersion);
        psi.ArgumentList.Add("-loader");
        psi.ArgumentList.Add(LauncherSettings.FabricLoaderVersion);
        psi.ArgumentList.Add("-noprofile");

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "Fabric Installer konnte nicht gestartet werden.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0 || !IsInstalled())
        {
            throw new InvalidOperationException(
                "Fabric-Installation fehlgeschlagen.\n" +
                stdout + "\n" + stderr);
        }
    }

    private static string? FindJava()
    {
        var candidates = new List<string>();

        var localRuntime = Path.Combine(AppPaths.Game, "runtime");

        if (Directory.Exists(localRuntime))
        {
            candidates.AddRange(
                Directory.EnumerateFiles(
                    localRuntime,
                    "java.exe",
                    SearchOption.AllDirectories));
        }

        var normalMinecraftRuntime = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            ".minecraft",
            "runtime");

        if (Directory.Exists(normalMinecraftRuntime))
        {
            candidates.AddRange(
                Directory.EnumerateFiles(
                    normalMinecraftRuntime,
                    "java.exe",
                    SearchOption.AllDirectories));
        }

        // Prefer newest-looking runtime path.
        return candidates
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static async Task<string> GetFabricInstallerAsync(
        Action<string>? status)
    {
        Directory.CreateDirectory(AppPaths.Cache);

        status?.Invoke("Lade Fabric Installer …");

        const string metadataUrl =
            "https://maven.fabricmc.net/net/fabricmc/fabric-installer/maven-metadata.xml";

        var metadataXml = await Http.GetStringAsync(metadataUrl);
        var document = XDocument.Parse(metadataXml);

        var version =
            document.Descendants("release").FirstOrDefault()?.Value ??
            document.Descendants("latest").FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidDataException(
                "Fabric Installer-Version konnte nicht ermittelt werden.");

        var target = Path.Combine(
            AppPaths.Cache,
            $"fabric-installer-{version}.jar");

        if (File.Exists(target))
            return target;

        var url =
            $"https://maven.fabricmc.net/net/fabricmc/fabric-installer/{version}/fabric-installer-{version}.jar";

        var bytes = await Http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(target, bytes);

        return target;
    }
}
