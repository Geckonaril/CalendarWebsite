namespace Takvim.Api.Models;

public class Calendar
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ColorHex { get; set; } = "#2196F3";

    public int UserId { get; set; }

    public AppUser User { get; set; } = null!;

    public List<CalendarEvent> Events { get; set; } = [];
}