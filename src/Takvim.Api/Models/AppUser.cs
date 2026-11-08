namespace Takvim.Api.Models;

public class AppUser
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Calendar> Calendars { get; set; } = [];
}
