using System.ComponentModel.DataAnnotations;

namespace Blink.ApiService.Database;

public sealed class BlinkUser
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [MaxLength(200)]
    public required string EmailAddress { get; set; }
    
    [MaxLength(30)]
    public string? FirstName { get; set; }
    
    [MaxLength(30)]
    public string? LastName { get; set; }
    
    [MaxLength(60)]
    public string? DisplayName { get; set; }
}