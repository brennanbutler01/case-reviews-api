using case_reviews_server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace case_reviews_server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class ReviewController(ApplicationDbContext db) : ControllerBase
{
    private string Owner => User.FindFirst("sub")!.Value;
    private IQueryable<Review> Owned => db.Reviews.Where(r => r.ReviewedBy == Owner)
        .Include(r => r.ReviewElements).Include(r => r.MagiEligibles);
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await Owned.AsNoTracking().ToListAsync());
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUnique(Guid id)
    {
        var review = await Owned.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id);
        return review == null ? NotFound() : Ok(review);
    }
    private async Task<bool> Valid(Review input) => Enum.IsDefined(input.Program)
        && input.CaseNumber > 0 && input.ReviewDate != default
        && input.ReviewElements != null && input.ReviewElements.Count > 0
        && input.ReviewElements.All(e => e != null && Enum.IsDefined(e.ReviewedElement) && e.Program == input.Program)
        && input.ReviewElements.Select(e => e.ReviewedElement).Distinct().Count() == input.ReviewElements.Count
        && (input.ReportingSystem == null || Enum.IsDefined(input.ReportingSystem.Value))
        && (input.MagiSubProgram == null || Enum.IsDefined(input.MagiSubProgram.Value))
        && (input.NonMagiSubProgram == null || Enum.IsDefined(input.NonMagiSubProgram.Value))
        && await db.Staff.AnyAsync(s => s.Id == input.StaffId && s.CreatedBy == Owner);
    private static void Copy(Review source, Review target)
    {
        target.Program = source.Program; target.ReviewDate = source.ReviewDate.ToUniversalTime();
        target.StaffId = source.StaffId; target.IsTargeted = source.IsTargeted;
        target.IsComplete = source.IsComplete; target.CaseNumber = source.CaseNumber;
        target.OtherComments = source.OtherComments; target.ReportingSystem = source.ReportingSystem;
        target.MagiSubProgram = source.MagiSubProgram; target.NonMagiSubProgram = source.NonMagiSubProgram;
        // Fresh child IDs prevent input from reparenting another user's records.
        target.ReviewElements = source.ReviewElements.Select(e => new ReviewElement {
            Id = Guid.NewGuid(), ReviewId = target.Id, ReviewedElement = e.ReviewedElement,
            Program = e.Program, IsReviewed = e.IsReviewed, IsError = e.IsError,
            HasAction = e.HasAction, Comments = e.Comments ?? string.Empty
        }).ToList();
        var m = source.MagiEligibles;
        target.MagiEligibles = m == null ? null : new MagiEligibles {
            Id = Guid.NewGuid(), ReviewId = target.Id,
            AdultsEligibleActual = m.AdultsEligibleActual, AdultsEligibleCoded = m.AdultsEligibleCoded,
            AdultsNotEligibleActual = m.AdultsNotEligibleActual, AdultsNotEligibleCoded = m.AdultsNotEligibleCoded,
            ChildrenEligibleActual = m.ChildrenEligibleActual, ChildrenEligibleCoded = m.ChildrenEligibleCoded,
            ChildrenNotEligibleActual = m.ChildrenNotEligibleActual, ChildrenNotEligibleCoded = m.ChildrenNotEligibleCoded
        };
    }
    [HttpPost]
    public async Task<IActionResult> Create(Review input)
    {
        if (!await Valid(input)) return BadRequest("A valid review and a staff member belonging to you are required.");
        var review = new Review { Id = Guid.NewGuid(), ReviewedBy = Owner };
        Copy(input, review); db.Reviews.Add(review); await db.SaveChangesAsync();
        return Created($"/Review/{review.Id}", review);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Review input)
    {
        var review = await Owned.SingleOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        if (input.Id != Guid.Empty && input.Id != id) return BadRequest("Route and body IDs differ.");
        if (!await Valid(input)) return BadRequest("A valid review and a staff member belonging to you are required.");
        db.ReviewElements.RemoveRange(review.ReviewElements);
        if (review.MagiEligibles != null) db.MagiEligibles.Remove(review.MagiEligibles);
        Copy(input, review);
        db.ReviewElements.AddRange(review.ReviewElements);
        if (review.MagiEligibles != null) db.MagiEligibles.Add(review.MagiEligibles);
        await db.SaveChangesAsync(); return Ok(review);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await Owned.SingleOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        db.ReviewElements.RemoveRange(review.ReviewElements);
        if (review.MagiEligibles != null) db.MagiEligibles.Remove(review.MagiEligibles);
        db.Reviews.Remove(review); await db.SaveChangesAsync(); return Ok(review);
    }
}
