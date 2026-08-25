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

        _launcher = new MinecraftLauncher(
            new MinecraftPath(AppPaths.Game));

        _loginHandler = new JELoginHandlerBuilder()
            .WithAccountManager(
                Path.Combine(AppPaths.State, "accounts.json"))
            .Build();
    }

    public async Task<MSession> LoginAsync()
    {
        return await _loginHandler.Authenticate();
    }

    public async Task EnsureMinecraftAsync(
        Action<string>? status = null)
    {
        status?.Invoke("Prüfe Minecraft 1.21 …");

        await _launcher.InstallAsync(
            LauncherSettings.MinecraftVersion);

        status?.Invoke("Minecraft 1.21 ist bereit.");
    }

    public async Task<Process> CreateFabricProcessAsync(
        MSession session)
    {
        await _launcher.GetAllVersionsAsync();

        var option = new MLaunchOption
        {
            Session = session,
            MaximumRamMb = LauncherSettings.MaximumRamMb,
            MinimumRamMb = 2048,
            GameLauncherName = "LeipzigCraft",
            GameLauncherVersion = "0.1.0"
        };

        return await _launcher.BuildProcessAsync(
            LauncherSettings.FabricVersionId,
            option);
    }
}
