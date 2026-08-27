namespace LeipzigCraft.Launcher.Models;

public sealed class PackManifest
{
    public int SchemaVersion { get; set; } = 2;
    public string Version { get; set; } = "";
    public string Minecraft { get; set; } = "";
    public string FabricLoader { get; set; } = "";
    public List<PackFile> Files { get; set; } = new();
    public PackArchive? ConfigArchive { get; set; }
}

public sealed class PackFile
{
    public string Path { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Size { get; set; }
}

public sealed class PackArchive
{
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Size { get; set; }
    public string ExtractTo { get; set; } = "config";
}
