namespace AddiScan.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConsentGivenAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>When true, grading totals for this user drop the functional necessity modifier instead of applying it.</summary>
    public bool IgnoreFunctionalNecessity { get; set; }
}
