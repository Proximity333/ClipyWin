using System;
using System.IO;

namespace ClipyWin.Utilities;

public static class AppPaths
{
    public static string AppDataFolder
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = Path.Combine(root, Constants.Application.Name);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string ClipsFolder
    {
        get
        {
            var path = Path.Combine(AppDataFolder, "Clips");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string ThumbnailsFolder
    {
        get
        {
            var path = Path.Combine(AppDataFolder, "Thumbnails");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string DatabaseFile => Path.Combine(AppDataFolder, "clipy.db");

    public static string SettingsFile => Path.Combine(AppDataFolder, "settings.json");
}
