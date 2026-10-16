namespace RaceDay.Api.Models;

public class Role
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = "";
}

public class AppUser
{
    public int UserID { get; set; }
    public int RoleID { get; set; }
    public Role Role { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public AppUser Organiser { get; set; } = null!;
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public string EventType { get; set; } = "Run";   // Run, Walk or Cycle
    public decimal DistanceKm { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Category> Categories { get; set; } = new();
}

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public Event Event { get; set; } = null!;
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; } = 100;
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public AppUser Participant { get; set; } = null!;
    public int CategoryID { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Confirmed";
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}
