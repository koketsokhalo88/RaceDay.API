namespace RaceDayMVC.ViewModels;

public class EventVm { public int Id { get; set; } public string EventName { get; set; } = ""; public string? Description { get; set; } public DateTime EventDate { get; set; } public string Location { get; set; } = ""; public string? EventType { get; set; } public string? BannerUrl { get; set; } public int OrganiserID { get; set; } public List<CategoryVm>? Categories { get; set; } }
public class EventFormVm { public string EventName { get; set; } = ""; public DateTime EventDate { get; set; } public string Location { get; set; } = ""; public string? Description { get; set; } public string? EventType { get; set; } }
public class CategoryVm { public int Id { get; set; } public string CategoryName { get; set; } = ""; public double? DistanceKm { get; set; } public int? MaxParticipants { get; set; } public int EventId { get; set; }
    public EventVm? Event { get; internal set; }
}
public class CategoryFormVm { public string CategoryName { get; set; } = ""; public double? DistanceKm { get; set; } public int? MaxParticipants { get; set; } }
public class EnrolmentVm { public int Id { get; set; } public int CategoryId { get; set; } public CategoryVm? Category { get; set; } public DateTime EnrolledAt { get; set; } public UserVm? User { get; set; } public ResultVm? Result { get; set; } }
public class ResultVm { public int Id { get; set; } public TimeSpan? FinishTime { get; set; } public int? Position { get; set; } public string? Status { get; set; } }
public class ResultFormVm { public TimeSpan? FinishTime { get; set; } public int? Position { get; set; } public string? Status { get; set; } }
public class UserVm { public int Id { get; set; } public string FullName { get; set; } = ""; public string Email { get; set; } = ""; }
public class ProfileVm { public int Id { get; set; } public string FullName { get; set; } = ""; public string Email { get; set; } = ""; public string Role { get; set; } = ""; }
public class ProfileFormVm { public string FullName { get; set; } = ""; public string Email { get; set; } = ""; }
