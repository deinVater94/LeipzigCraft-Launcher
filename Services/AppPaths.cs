namespace LeipzigCraft.Launcher.Services;

public static class AppPaths
{
    public static readonly string Root =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LeipzigCraft");

    public static readonly string Game = Path.Combine(Root, "game");
    public static readonly string Cache = Path.Combine(Root, "cache");
    public static readonly string State = Path.Combine(Root, "state");

    public static void Ensure()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Game);
        Directory.CreateDirectory(Cache);
        Directory.CreateDirectory(State);
    }
}
