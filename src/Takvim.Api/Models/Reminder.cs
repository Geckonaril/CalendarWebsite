namespace Takvim.Api.Models;

public class Reminder
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public CalendarEvent Event { get; set; } = null!;

    public DateTime RemindAtUtc { get; set; }

    public bool IsSent { get; set; } = false;
}