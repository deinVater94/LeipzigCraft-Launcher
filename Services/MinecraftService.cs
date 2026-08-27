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

    private static readonly string RunningPidFile =
        Path.Combine(AppPaths.State, "minecraft.pid");

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

    public bool IsLeipzigCraftRunning()
    {
        AppPaths.Ensure();

        if (!File.Exists(RunningPidFile))
            return false;

        try
        {
            var text = File.ReadAllText(RunningPidFile).Trim();

            if (!int.TryParse(text, out var pid))
            {
                File.Delete(RunningPidFile);
                return false;
            }

            var process = Process.GetProcessById(pid);

            if (process.HasExited)
            {
                File.Delete(RunningPidFile);
                return false;
            }

            return true;
        }
        catch
        {
            if (File.Exists(RunningPidFile))
                File.Delete(RunningPidFile);

            return false;
        }
    }

    public async Task EnsureMinecraftAsync(Action<string>? status = null)
    {
        status?.Invoke("Installiere / prüfe Minecraft 1.21 …");
        await _launcher.InstallAsync(LauncherSettings.MinecraftVersion);
        status?.Invoke("Minecraft 1.21 ist vollständig installiert.");
    }

    public async Task<Process> CreateFabricProcessAsync(
        MSession session,
        bool fabricRuntimeAlreadyInstalled,
        Action<string>? status = null)
    {
        status?.Invoke("Prüfe Fabric- und Minecraft-Dateien …");
        await _launcher.GetAllVersionsAsync();

        var option = new MLaunchOption
        {
            Session = session,
            MaximumRamMb = LauncherSettings.MaximumRamMb,
            MinimumRamMb = 2048,
            GameLauncherName = "LeipzigCraft",
            GameLauncherVersion = "0.2.1"
        };

        Process process;

        if (fabricRuntimeAlreadyInstalled)
        {
            status?.Invoke("Fabric ist aktuell. Bereite Start vor …");
            process = await _launcher.BuildProcessAsync(
                LauncherSettings.FabricVersionId,
                option);
        }
        else
        {
            status?.Invoke("Installiere benötigte Fabric-Dateien …");
            process = await _launcher.InstallAndBuildProcessAsync(
                LauncherSettings.FabricVersionId,
                option);
        }

        WriteLaunchDebugInfo(process);
        return process;
    }

    public void RegisterRunningProcess(Process process)
    {
        AppPaths.Ensure();
        File.WriteAllText(RunningPidFile, process.Id.ToString());

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            try
            {
                if (!File.Exists(RunningPidFile)) return;

                var pidText = File.ReadAllText(RunningPidFile).Trim();
                if (pidText == process.Id.ToString())
                    File.Delete(RunningPidFile);
            }
            catch
            {
            }
        };
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

            File.WriteAllText(Path.Combine(AppPaths.State, "last-launch.txt"), debug);
        }
        catch
        {
        }
    }
}
