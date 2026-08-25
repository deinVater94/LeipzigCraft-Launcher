namespace LeipzigCraft.Launcher.Models;

public static class LauncherSettings
{
    public const string MinecraftVersion = "1.21";
    public const string FabricLoaderVersion = "0.18.4";
    public const string FabricVersionId = "fabric-loader-0.18.4-1.21";

    // Small JSON file hosted by the LeipzigCraft website.
    public const string PackManifestUrl = "https://leipzigcraft.com/launcher/pack.json";

    public const int MaximumRamMb = 6144;

    // Fill this in later if you want one-click direct server joining.
    public const string ServerAddress = "";
}
