namespace Blink.Web.Components.Shared;

public sealed record MentionMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public int Length { get; set; }
    public bool IsNewPerson { get; set; }
}
