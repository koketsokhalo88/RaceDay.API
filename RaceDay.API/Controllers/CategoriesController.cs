using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    /// <summary>Updates a category. Organiser only, owner of the event only.</summary>
    [HttpPut("{id:int}"), SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryDto dto)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (cat is null) return NotFound(new { message = "Category not found." });
        if (cat.Event.OrganiserID != CurrentUserId) return Forbidden("Only the event's organiser may change this category.");

        cat.CategoryName = dto.CategoryName.Trim();
        cat.DistanceKm = dto.DistanceKm;
        cat.MaxParticipants = dto.MaxParticipants;
        await _db.SaveChangesAsync();
        return Ok(Map.ToCategory(cat));
    }

    /// <summary>Removes a category from an event. Organiser only, owner only.</summary>
    [HttpDelete("{id:int}"), SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.Include(c => c.Event)
            .Include(c => c.Enrolments).ThenInclude(e => e.Result)
            .FirstOrDefaultAsync(c => c.CategoryID == id);
        if (cat is null) return NotFound(new { message = "Category not found." });
        if (cat.Event.OrganiserID != CurrentUserId) return Forbidden("Only the event's organiser may delete this category.");

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Enrols the logged-in participant into a category. Participant only.</summary>
    /// <remarks>No body needed. 201 on success, 404 if the category does not exist, 409 if the category is full or the participant is already enrolled.</remarks>
    [HttpPost("{id:int}/enrolments"), SessionAuth(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is null) return NotFound(new { message = "Category does not exist." });

        if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id && e.ParticipantID == CurrentUserId))
            return Conflict(new { message = "You are already enrolled in this category." });

        var count = await _db.Enrolments.CountAsync(e => e.CategoryID == id);
        if (count >= cat.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment { ParticipantID = CurrentUserId, CategoryID = id };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();
        return Created($"/api/enrolments/{enrolment.EnrolmentID}", Map.ToEnrolment(enrolment));
    }

    /// <summary>Lists everyone enrolled in a category. Organiser only, owner of the event only.</summary>
    [HttpGet("{id:int}/enrolments"), SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolments(int id)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (cat is null) return NotFound(new { message = "Category not found." });
        if (cat.Event.OrganiserID != CurrentUserId) return Forbidden("Only the event's organiser may view its enrolments.");

        var list = await _db.Enrolments.AsNoTracking().Include(e => e.Participant)
            .Where(e => e.CategoryID == id).ToListAsync();
        return Ok(list.Select(e => new
        {
            enrolmentId = e.EnrolmentID,
            participantId = e.ParticipantID,
            participantName = e.Participant.FullName,
            participantEmail = e.Participant.Email,
            enrolmentDate = e.EnrolmentDate,
            status = e.Status
        }));
    }

    /// <summary>Gets the results leaderboard for a category, ordered by position. Public.</summary>
    [HttpGet("{id:int}/results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResults(int id)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryID == id))
            return NotFound(new { message = "Category not found." });

        var results = await _db.Results.AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Where(r => r.Enrolment.CategoryID == id).ToListAsync();

        return Ok(results.OrderBy(r => r.Position ?? int.MaxValue).Select(r => new
        {
            position = r.Position,
            participantName = r.Enrolment.Participant.FullName,
            finishTime = r.FinishTime,
            status = r.Status
        }));
    }
}
