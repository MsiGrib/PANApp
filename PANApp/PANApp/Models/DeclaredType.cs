namespace PANApp.Models;

public sealed record DeclaredType
{
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}