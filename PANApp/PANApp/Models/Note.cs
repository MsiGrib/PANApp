using System;

namespace PANApp.Models;

public sealed record Note
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long NoteNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ProfileId { get; set; } = string.Empty;
    public string ModuleId { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
}