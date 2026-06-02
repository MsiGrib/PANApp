using PANApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PANApp.Services;

public class ProfileService
{
    private readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PANApp", "profiles.json");

    public ProfileService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public void SaveProfiles(List<ProjectProfile> profiles)
    {
        var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public List<ProjectProfile> LoadProfiles()
    {
        if (!File.Exists(_filePath)) return new List<ProjectProfile>();
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<ProjectProfile>>(json) ?? new List<ProjectProfile>();
    }
}