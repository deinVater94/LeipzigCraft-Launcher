using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using LeipzigCraft.Launcher.Models;
using XboxAuthNet.Game.Msal;

namespace LeipzigCraft.Launcher.Services;

public sealed class MinecraftService
{
    private readonly MinecraftLauncher _launcher;

    public MinecraftService()
    {
        AppPaths.Ensure();

        _launcher = new MinecraftLauncher(
            new MinecraftPath(AppPaths.Game));
    }

    public async Task<MSession> LoginAsync()
    {
        // XboxAuthNet handles Microsoft/Xbox/Minecraft authentication
        // and uses its cached account on later launches where possible.
        var loginHandler = JELoginHandlerBuilder.BuildDefault();

        return await loginHandler.Authenticate();
    }

    public async Task EnsureMinecraftAsync(
        Action<string>? status = null)
    {
        status?.Invoke("Prüfe Minecraft 1.21 …");

        // CmlLib downloads the official version files, libraries,
        // assets and Mojang Java runtime into our isolated game folder.
        await _launcher.InstallAsync(
            LauncherSettings.MinecraftVersion);

        status?.Invoke("Minecraft 1.21 ist bereit.");
    }

    public async Task<Process> CreateFabricProcessAsync(
        MSession session)
    {
        var option = new MLaunchOption
        {
            Session = session,
            MaximumRamMb = LauncherSettings.MaximumRamMb,
            MinimumRamMb = 2048
        };

        return await _launcher.CreateProcessAsync(
            LauncherSettings.FabricVersionId,
            option);
    }
}
