using PANApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PANApp.Services.Implementations;

public class ProfileService
{
    private static readonly string ConfigDir = GetConfigDir();
    private readonly string _filePath = Path.Combine(ConfigDir, "profiles.json");

    public ProfileService()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Create config directory: {ex.Message}");
        }
    }

    #region Config paths

    private static string GetConfigDir()
    {
        try
        {
            var appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create
            );
            if (!string.IsNullOrEmpty(appData))
                return Path.Combine(appData, "PANApp");
        }
        catch { }

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfig))
                return Path.Combine(xdgConfig, "PANApp");

            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                return Path.Combine(home, ".config", "PANApp");
        }

        return Path.Combine(AppContext.BaseDirectory, "config");
    }

    #endregion

    public void SaveProfiles(List<ProjectProfile> profiles)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            Console.WriteLine($"[PROFILES] Saved to: {_filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Save profiles: {ex.Message}");
        }
    }

    public List<ProjectProfile> LoadProfiles()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<ProjectProfile>();

            var json = File.ReadAllText(_filePath);
            var profiles = JsonSerializer.Deserialize<List<ProjectProfile>>(json);

            return profiles ?? new List<ProjectProfile>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Load profiles: {ex.Message}");
            return new List<ProjectProfile>();
        }
    }
}