using System;
using System.IO;

namespace TheRichScript_WinUI.Services;

public static class AppStorageHelper
{
    public static string GetAppDataFolder()
    {
        string baseFolder;
        try
        {
            // Check if running inside packaged MSIX container
            baseFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        catch
        {
            // Fallback for unpackaged execution
            baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        var appFolder = Path.Combine(baseFolder, "TheRichScript");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        return appFolder;
    }

    public static string GetFilePath(string fileName)
    {
        return Path.Combine(GetAppDataFolder(), fileName);
    }
}
