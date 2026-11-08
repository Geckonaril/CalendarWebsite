namespace Takvim.Api.Models;

public class CalendarEvent
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartAtUtc { get; set; }

    public DateTime EndAtUtc { get; set; }

    public bool IsAllDay { get; set; }

    public string? Location { get; set; }

    public int CalendarId { get; set; }

    public Calendar Calendar { get; set; } = null!;

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public List<Reminder> Reminders { get; set; } = [];
}