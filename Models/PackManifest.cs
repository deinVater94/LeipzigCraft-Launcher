namespace LeipzigCraft.Launcher.Models;

public sealed class PackManifest
{
    public string Version { get; set; } = "";
    public string ZipUrl { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Size { get; set; }
}
