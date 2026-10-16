using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : ApiControllerBase
{
    private static readonly string[] Statuses = { "Finished", "DNF", "DNS" };
    private readonly AppDbContext _db;
    public EnrolmentsController(AppDbContext db) => _db = db;

    /// <summary>Cancels an enrolment. Participant only, and only the enrolled participant.</summary>
    [HttpDelete("{id:int}"), SessionAuth(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var en = await _db.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (en is null) return NotFound(new { message = "Enrolment not found." });
        if (en.ParticipantID != CurrentUserId) return Forbidden("You can only cancel your own enrolment.");

        _db.Enrolments.Remove(en);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Records the finishing result for an enrolment. Organiser only, owner of the event only.</summary>
    /// <remarks>Expects: finishTime (HH:mm:ss), position, status (Finished, DNF or DNS). 409 if a result already exists.</remarks>
    [HttpPost("{id:int}/result"), SessionAuth(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordResult(int id, ResultDto dto)
    {
        var status = Statuses.FirstOrDefault(s => s.Equals(dto.Status, StringComparison.OrdinalIgnoreCase));
        if (status is null) return BadRequest(new { message = "Status must be Finished, DNF or DNS." });

        var en = await _db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (en is null) return NotFound(new { message = "Enrolment does not exist." });
        if (en.Category.Event.OrganiserID != CurrentUserId) return Forbidden("Only the event's organiser may record results.");
        if (en.Result is not null) return Conflict(new { message = "A result is already recorded for this enrolment." });

        var result = new Result { EnrolmentID = id, FinishTime = dto.FinishTime, Position = dto.Position, Status = status };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();
        return Created($"/api/enrolments/{id}/result", Map.ToResult(result));
    }

    /// <summary>Gets the result for an enrolment. Any logged-in user: participants see only their own, organisers only their own events'.</summary>
    [HttpGet("{id:int}/result"), SessionAuth]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(int id)
    {
        var en = await _db.Enrolments.AsNoTracking().Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (en is null) return NotFound(new { message = "Enrolment not found." });

        if (CurrentRole == Roles.Participant && en.ParticipantID != CurrentUserId)
            return Forbidden("You can only view your own results.");
        if (CurrentRole == Roles.Organiser && en.Category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only view results for your own events.");

        return en.Result is null
            ? NotFound(new { message = "No result recorded yet." })
            : Ok(Map.ToResult(en.Result));
    }
}