namespace Takvim.Api.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ColorHex { get; set; } = "#4CAF50";

    public List<CalendarEvent> Events { get; set; } = [];
}