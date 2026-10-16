using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegisterDto
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "";
}

public class LoginDto
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileDto
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
}

public class EventDto
{
    [Required, StringLength(150)] public string EventName { get; set; } = "";
    [Required] public DateOnly? EventDate { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public string EventType { get; set; } = "";
    [Range(0.1, 500)] public decimal DistanceKm { get; set; }
}

public class CategoryDto
{
    [Required, StringLength(100)] public string CategoryName { get; set; } = "";
    [Range(0.1, 500)] public decimal DistanceKm { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 100;
}

public class ResultDto
{
    [RegularExpression(@"^\d{2}:\d{2}:\d{2}$", ErrorMessage = "FinishTime must be HH:mm:ss.")]
    public string? FinishTime { get; set; }
    [Range(1, 100000)] public int? Position { get; set; }
    [Required] public string Status { get; set; } = "Finished";
}

/// <summary>Shapes entities for responses (so the password hash is never returned).</summary>
public static class Map
{
    public static object ToUser(AppUser u, string role) => new
    { userId = u.UserID, fullName = u.FullName, email = u.Email, role, createdAt = u.CreatedAt };

    public static object ToEvent(Event e) => new
    {
        eventId = e.EventID,
        organiserId = e.OrganiserID,
        eventName = e.EventName,
        eventDate = e.EventDate,
        location = e.Location,
        description = e.Description,
        eventType = e.EventType,
        distanceKm = e.DistanceKm
    };

    public static object ToEventWithCategories(Event e) => new
    {
        eventId = e.EventID,
        organiserId = e.OrganiserID,
        eventName = e.EventName,
        eventDate = e.EventDate,
        location = e.Location,
        description = e.Description,
        eventType = e.EventType,
        distanceKm = e.DistanceKm,
        categories = e.Categories.Select(ToCategory)
    };

    public static object ToCategory(Category c) => new
    { categoryId = c.CategoryID, eventId = c.EventID, categoryName = c.CategoryName, distanceKm = c.DistanceKm, maxParticipants = c.MaxParticipants };

    public static object ToEnrolment(Enrolment e) => new
    { enrolmentId = e.EnrolmentID, participantId = e.ParticipantID, categoryId = e.CategoryID, enrolmentDate = e.EnrolmentDate, status = e.Status };

    public static object ToResult(Result r) => new
    { resultId = r.ResultID, enrolmentId = r.EnrolmentID, finishTime = r.FinishTime, position = r.Position, status = r.Status };
}
