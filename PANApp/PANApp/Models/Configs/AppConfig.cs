using System;
using System.Text.Json.Serialization;

namespace PANApp.Models.Configs;

public sealed record AppConfig
{
    [JsonPropertyName("StartWithWindows")]
    public bool StartWithWindows { get; set; } = false;

    [JsonPropertyName("Version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("LastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}