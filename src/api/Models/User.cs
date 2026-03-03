namespace WatchTracker.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? ProfileImage { get; set; }
    public UserRole Role { get; set; } = UserRole.Standard;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Watch> Watches { get; set; } = [];
}
