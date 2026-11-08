
using Microsoft.AspNetCore.Identity;

namespace Takvim.Api.Models;

public class AppUser : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public List<Calendar> Calendars { get; set; } = [];
}
