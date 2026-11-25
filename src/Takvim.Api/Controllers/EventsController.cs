using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Takvim.Api.Contracts;
using Takvim.Api.Data;
using Takvim.Api.Models;

namespace Takvim.Api.Controllers;

/// <summary>
/// Oturum açmış kullanıcının etkinlikleri için CRUD uç noktaları.
/// Kullanıcı yalnızca kendi takvimlerine ait etkinliklere erişebilir.
/// </summary>
[ApiController]
[Authorize]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private const string DefaultCalendarName = "Takvimim";
    private const string DefaultCalendarColor = "#3563E9";

    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Kullanıcının etkinliklerini listeler.
    /// from/to verilirse yalnızca bu aralıkla kesişen etkinlikler döner.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<EventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EventResponse>>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var fromUtc = from is null ? (DateTime?)null : ToUtc(from.Value);
        var toUtc = to is null ? (DateTime?)null : ToUtc(to.Value);

        if (fromUtc is not null && toUtc is not null && toUtc <= fromUtc)
        {
            ModelState.AddModelError(
                nameof(to), "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
            return ValidationProblem(ModelState);
        }

        var query = UserEvents(userId);

        // Bitiş zamanı hariç tutulur: 10:00'da biten etkinlik 10:00'dan
        // başlayan aralıkta görünmez. Süresiz (başlangıç = bitiş)
        // etkinlikler başlangıçlarına göre dahil edilir.
        if (fromUtc is not null)
            query = query.Where(e =>
                e.EndAtUtc > fromUtc || e.StartAtUtc >= fromUtc);

        if (toUtc is not null)
            query = query.Where(e => e.StartAtUtc < toUtc);

        var events = await query
            .OrderBy(e => e.StartAtUtc)
            .ThenBy(e => e.Id)
            .Select(ToResponse)
            .ToListAsync(ct);

        return Ok(events);
    }

    /// <summary>Tek bir etkinliğin ayrıntılarını getirir.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventResponse>> GetById(
        int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var ev = await UserEvents(userId)
            .Where(e => e.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(ct);

        return ev is null ? NotFound() : Ok(ev);
    }

    /// <summary>Yeni bir etkinlik oluşturur.</summary>
    [HttpPost]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventResponse>> Create(
        EventRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var calendarId = await ResolveCalendarIdAsync(
            userId, request.CalendarId, ct);

        if (calendarId is null || !await CategoryIsValidAsync(request.CategoryId, ct))
            return ValidationProblem(ModelState);

        var ev = new CalendarEvent { CalendarId = calendarId.Value };
        Apply(ev, request);

        _db.Events.Add(ev);
        await _db.SaveChangesAsync(ct);

        var response = await LoadResponseAsync(ev.Id, ct);

        return CreatedAtAction(
            nameof(GetById), new { id = ev.Id }, response);
    }

    /// <summary>Mevcut bir etkinliği günceller.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventResponse>> Update(
        int id, EventRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var ev = await UserEvents(userId)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (ev is null)
            return NotFound();

        // Takvim belirtilmezse etkinlik mevcut takviminde kalır.
        var calendarId = request.CalendarId is null
            ? ev.CalendarId
            : await ResolveCalendarIdAsync(userId, request.CalendarId, ct);

        if (calendarId is null || !await CategoryIsValidAsync(request.CategoryId, ct))
            return ValidationProblem(ModelState);

        ev.CalendarId = calendarId.Value;
        Apply(ev, request);

        await _db.SaveChangesAsync(ct);

        return Ok(await LoadResponseAsync(ev.Id, ct));
    }

    /// <summary>Bir etkinliği siler.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var ev = await UserEvents(userId)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (ev is null)
            return NotFound();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ---------------------------------------------------------------
    // Yardımcılar
    // ---------------------------------------------------------------

    private static readonly System.Linq.Expressions.Expression<
        Func<CalendarEvent, EventResponse>> ToResponse = e =>
        new EventResponse(
            e.Id,
            e.Title,
            e.Description,
            e.StartAtUtc,
            e.EndAtUtc,
            e.IsAllDay,
            e.Location,
            e.CalendarId,
            e.Calendar.Name,
            e.Calendar.ColorHex,
            e.CategoryId,
            e.Category != null ? e.Category.Name : null,
            e.Category != null ? e.Category.ColorHex : null);

    private bool TryGetUserId(out int userId) =>
        int.TryParse(
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out userId);

    private IQueryable<CalendarEvent> UserEvents(int userId) =>
        _db.Events.Where(e => e.Calendar.UserId == userId);

    private Task<EventResponse> LoadResponseAsync(int id, CancellationToken ct) =>
        _db.Events
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(ToResponse)
            .FirstAsync(ct);

    private static void Apply(CalendarEvent ev, EventRequest request)
    {
        ev.Title = request.Title.Trim();
        ev.Description = NullIfEmpty(request.Description);
        ev.Location = NullIfEmpty(request.Location);
        ev.IsAllDay = request.IsAllDay;
        ev.StartAtUtc = ToUtc(request.StartAtUtc!.Value);
        ev.EndAtUtc = ToUtc(request.EndAtUtc!.Value);
        ev.CategoryId = request.CategoryId;
    }

    /// <summary>
    /// İstenen takvimin kullanıcıya ait olduğunu doğrular.
    /// Takvim belirtilmemişse varsayılan takvimi getirir veya oluşturur.
    /// </summary>
    private async Task<int?> ResolveCalendarIdAsync(
        int userId, int? requestedId, CancellationToken ct)
    {
        if (requestedId is not null)
        {
            var owns = await _db.Calendars.AnyAsync(
                c => c.Id == requestedId && c.UserId == userId, ct);

            if (owns)
                return requestedId;

            ModelState.AddModelError(
                nameof(EventRequest.CalendarId), "Takvim bulunamadı.");
            return null;
        }

        var existing = await _db.Calendars
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Id)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing;

        var calendar = new Calendar
        {
            Name = DefaultCalendarName,
            ColorHex = DefaultCalendarColor,
            UserId = userId
        };

        _db.Calendars.Add(calendar);
        await _db.SaveChangesAsync(ct);

        return calendar.Id;
    }

    private async Task<bool> CategoryIsValidAsync(
        int? categoryId, CancellationToken ct)
    {
        if (categoryId is null)
            return true;

        if (await _db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            return true;

        ModelState.AddModelError(
            nameof(EventRequest.CategoryId), "Kategori bulunamadı.");
        return false;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
