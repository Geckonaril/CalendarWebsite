using System.ComponentModel.DataAnnotations;

namespace Takvim.Api.Contracts;

/// <summary>
/// Etkinlik oluşturma ve güncelleme isteği.
/// Tüm zamanlar UTC olarak gönderilmelidir (ör. 2026-11-25T09:00:00Z).
/// </summary>
public class EventRequest : IValidatableObject
{
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Başlangıç zamanı zorunludur.")]
    public DateTime? StartAtUtc { get; set; }

    [Required(ErrorMessage = "Bitiş zamanı zorunludur.")]
    public DateTime? EndAtUtc { get; set; }

    public bool IsAllDay { get; set; }

    [StringLength(250, ErrorMessage = "Konum en fazla 250 karakter olabilir.")]
    public string? Location { get; set; }

    /// <summary>
    /// Boş bırakılırsa kullanıcının varsayılan takvimi kullanılır.
    /// </summary>
    public int? CalendarId { get; set; }

    public int? CategoryId { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult(
                "Başlık boş olamaz.",
                [nameof(Title)]);
        }

        if (StartAtUtc is { } start &&
            EndAtUtc is { } end &&
            end < start)
        {
            yield return new ValidationResult(
                "Bitiş zamanı başlangıç zamanından önce olamaz.",
                [nameof(EndAtUtc)]);
        }
    }
}

public record EventResponse(
    int Id,
    string Title,
    string? Description,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    bool IsAllDay,
    string? Location,
    int CalendarId,
    string CalendarName,
    string CalendarColor,
    int? CategoryId,
    string? CategoryName,
    string? CategoryColor
);
