using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };
    private readonly AppDbContext _db;
    public EventsController(AppDbContext db) => _db = db;

    /// <summary>Creates a new event. Organiser only.</summary>
    /// <remarks>Expects: eventName, eventDate (yyyy-MM-dd), location, description, eventType (Run, Walk or Cycle), distanceKm.</remarks>
    [HttpPost, SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventDto dto)
    {
        var type = EventTypes.FirstOrDefault(t => t.Equals(dto.EventType, StringComparison.OrdinalIgnoreCase));
        if (type is null) return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            EventName = dto.EventName.Trim(),
            EventDate = dto.EventDate!.Value,
            Location = dto.Location.Trim(),
            Description = dto.Description,
            EventType = type,
            DistanceKm = dto.DistanceKm
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, Map.ToEvent(ev));
    }

    /// <summary>Lists all events. Public.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.AsNoTracking().OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(Map.ToEvent));
    }

    /// <summary>Gets one event including its categories. Public. 404 if not found.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        return ev is null ? NotFound(new { message = "Event not found." }) : Ok(Map.ToEventWithCategories(ev));
    }

    /// <summary>Updates an event. Organiser only, and only the organiser who created it (403 otherwise).</summary>
    [HttpPut("{id:int}"), SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId) return Forbidden("Only the organiser who created this event may change it.");

        var type = EventTypes.FirstOrDefault(t => t.Equals(dto.EventType, StringComparison.OrdinalIgnoreCase));
        if (type is null) return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        ev.EventName = dto.EventName.Trim();
        ev.EventDate = dto.EventDate!.Value;
        ev.Location = dto.Location.Trim();
        ev.Description = dto.Description;
        ev.EventType = type;
        ev.DistanceKm = dto.DistanceKm;
        await _db.SaveChangesAsync();
        return Ok(Map.ToEvent(ev));
    }

    /// <summary>Deletes an event and its categories. Organiser only, owner only.</summary>
    [HttpDelete("{id:int}"), SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments).ThenInclude(en => en.Result)
            .FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId) return Forbidden("Only the organiser who created this event may delete it.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adds a race category (e.g. 10km, Under 20) to an event. Organiser only, owner only.</summary>
    /// <remarks>Expects: categoryName, distanceKm, maxParticipants.</remarks>
    [HttpPost("{id:int}/categories"), SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCategory(int id, CategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != CurrentUserId) return Forbidden("Only the organiser who created this event may add categories.");

        var cat = new Category
        {
            EventID = id,
            CategoryName = dto.CategoryName.Trim(),
            DistanceKm = dto.DistanceKm,
            MaxParticipants = dto.MaxParticipants
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return Created($"/api/events/{id}/categories", Map.ToCategory(cat));
    }

    /// <summary>Lists all categories for an event. Public.</summary>
    [HttpGet("{id:int}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id))
            return NotFound(new { message = "Event not found." });
        var cats = await _db.Categories.AsNoTracking().Where(c => c.EventID == id).ToListAsync();
        return Ok(cats.Select(Map.ToCategory));
    }
}
