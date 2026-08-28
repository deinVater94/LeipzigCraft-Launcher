using System.IO;
using fNbt;
using LeipzigCraft.Launcher.Models;

namespace LeipzigCraft.Launcher.Services;

public static class ServerListService
{
    private static string ServersFile =>
        Path.Combine(AppPaths.Game, "servers.dat");

    /// <summary>
    /// Ensures LeipzigCraft is visible as the first server in Minecraft's
    /// multiplayer list while preserving all other saved servers.
    /// </summary>
    public static void EnsureLeipzigCraftFirst()
    {
        AppPaths.Ensure();

        NbtFile file;

        if (File.Exists(ServersFile))
        {
            try
            {
                file = new NbtFile();
                file.LoadFromFile(
                    ServersFile,
                    NbtCompression.None);
            }
            catch
            {
                BackupBrokenServerList();

                file = CreateEmptyServerFile();
            }
        }
        else
        {
            file = CreateEmptyServerFile();
        }

        var root = file.RootTag;

        NbtList servers;

        if (root.Contains("servers") &&
            root["servers"] is NbtList existingList &&
            existingList.ListType == NbtTagType.Compound)
        {
            servers = existingList;
        }
        else
        {
            if (root.Contains("servers"))
                root.Remove("servers");

            servers = new NbtList(
                "servers",
                NbtTagType.Compound);

            root.Add(servers);
        }

        NbtCompound? previousLeipzigCraft = null;

        // Remove older LeipzigCraft entries so there is exactly one,
        // then insert the current entry at index 0.
        for (var i = servers.Count - 1; i >= 0; i--)
        {
            if (servers[i] is not NbtCompound server)
                continue;

            var name = GetString(server, "name");
            var ip = GetString(server, "ip");

            if (string.Equals(
                    ip,
                    LauncherSettings.ServerAddress,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    name,
                    LauncherSettings.ServerName,
                    StringComparison.OrdinalIgnoreCase))
            {
                previousLeipzigCraft ??= server;
                servers.RemoveAt(i);
            }
        }

        var leipzigCraft = new NbtCompound
        {
            new NbtString(
                "name",
                LauncherSettings.ServerName),

            new NbtString(
                "ip",
                LauncherSettings.ServerAddress),

            // Always visible in Multiplayer.
            new NbtByte(
                "hidden",
                0)
        };

        // Preserve Minecraft's cached server icon, if one already exists.
        var icon =
            previousLeipzigCraft is null
                ? null
                : GetString(previousLeipzigCraft, "icon");

        if (!string.IsNullOrWhiteSpace(icon))
        {
            leipzigCraft.Add(
                new NbtString(
                    "icon",
                    icon));
        }

        // Preserve the player's previous server-resource-pack choice.
        // If there is no previous choice, Minecraft asks normally.
        if (previousLeipzigCraft is not null &&
            TryGetByte(
                previousLeipzigCraft,
                "acceptTextures",
                out var acceptTextures))
        {
            leipzigCraft.Add(
                new NbtByte(
                    "acceptTextures",
                    acceptTextures));
        }

        servers.Insert(0, leipzigCraft);

        SaveAtomically(file);
    }

    private static NbtFile CreateEmptyServerFile()
    {
        var root = new NbtCompound("");

        root.Add(
            new NbtList(
                "servers",
                NbtTagType.Compound));

        return new NbtFile(root);
    }

    private static string? GetString(
        NbtCompound compound,
        string name)
    {
        if (!compound.Contains(name))
            return null;

        return compound[name] is NbtString value
            ? value.Value
            : null;
    }

    private static bool TryGetByte(
        NbtCompound compound,
        string name,
        out byte value)
    {
        value = 0;

        if (!compound.Contains(name) ||
            compound[name] is not NbtByte tag)
        {
            return false;
        }

        value = tag.Value;
        return true;
    }

    private static void SaveAtomically(NbtFile file)
    {
        var temp =
            ServersFile + ".leipzigcraft.tmp";

        try
        {
            file.SaveToFile(
                temp,
                NbtCompression.None);

            File.Move(
                temp,
                ServersFile,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static void BackupBrokenServerList()
    {
        try
        {
            var backup =
                ServersFile +
                ".broken-" +
                DateTime.Now.ToString(
                    "yyyyMMdd-HHmmss") +
                ".bak";

            File.Copy(
                ServersFile,
                backup,
                overwrite: false);
        }
        catch
        {
            // A broken server list must never prevent LeipzigCraft from starting.
        }
    }
}
