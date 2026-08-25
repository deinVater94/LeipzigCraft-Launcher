using System.Diagnostics;
using System.IO;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ProcessBuilder;
using LeipzigCraft.Launcher.Models;

namespace LeipzigCraft.Launcher.Services;

public sealed class MinecraftService
{
    private readonly MinecraftLauncher _launcher;
    private readonly JELoginHandler _loginHandler;

    public MinecraftService()
    {
        AppPaths.Ensure();

        _launcher = new MinecraftLauncher(new MinecraftPath(AppPaths.Game));

        _loginHandler = new JELoginHandlerBuilder()
            .WithAccountManager(Path.Combine(AppPaths.State, "accounts.json"))
            .Build();
    }

    public async Task<MSession> LoginAsync()
    {
        return await _loginHandler.Authenticate();
    }

    public async Task EnsureMinecraftAsync(Action<string>? status = null)
    {
        status?.Invoke("Installiere / prüfe Minecraft 1.21 …");

        // Installs vanilla 1.21 completely: client JAR, libraries, assets and Java.
        await _launcher.InstallAsync(LauncherSettings.MinecraftVersion);

        status?.Invoke("Minecraft 1.21 ist vollständig installiert.");
    }

    public async Task<Process> CreateFabricProcessAsync(
        MSession session,
        Action<string>? status = null)
    {
        status?.Invoke("Prüfe Fabric- und Minecraft-Dateien …");

        // FabricService created a new custom version profile. Refresh versions.
        await _launcher.GetAllVersionsAsync();

        var option = new MLaunchOption
        {
            Session = session,
            MaximumRamMb = LauncherSettings.MaximumRamMb,
            MinimumRamMb = 2048,
            GameLauncherName = "LeipzigCraft",
            GameLauncherVersion = "0.1.1"
        };

        // Important: install the Fabric custom version too, instead of only
        // building arguments from files already present on disk.
        var process = await _launcher.InstallAndBuildProcessAsync(
            LauncherSettings.FabricVersionId,
            option);

        WriteLaunchDebugInfo(process);
        return process;
    }

    private static void WriteLaunchDebugInfo(Process process)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.State);

            var info = process.StartInfo;
            var debug =
                $"Generated: {DateTimeOffset.Now:O}{Environment.NewLine}" +
                $"FileName: {info.FileName}{Environment.NewLine}" +
                $"WorkingDirectory: {info.WorkingDirectory}{Environment.NewLine}" +
                $"Arguments: {info.Arguments}{Environment.NewLine}";

            File.WriteAllText(
                Path.Combine(AppPaths.State, "last-launch.txt"),
                debug);
        }
        catch
        {
        }
    }
}
