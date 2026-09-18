using case_reviews_server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace case_reviews_server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class StaffController(ApplicationDbContext db) : ControllerBase
{
    private string Owner => User.FindFirst("sub")!.Value;
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await db.Staff.AsNoTracking().Where(s => s.CreatedBy == Owner).ToListAsync());
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUnique(Guid id)
    {
        var staff = await db.Staff.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id && s.CreatedBy == Owner);
        return staff == null ? NotFound() : Ok(staff);
    }
    private static bool Valid(Staff staff) => !string.IsNullOrWhiteSpace(staff.FirstName)
        && !string.IsNullOrWhiteSpace(staff.LastName) && !string.IsNullOrWhiteSpace(staff.ORNumber)
        && Enum.IsDefined(staff.Office);
    private static void Copy(Staff source, Staff target)
    {
        target.FirstName = source.FirstName.Trim(); target.LastName = source.LastName.Trim();
        target.ORNumber = source.ORNumber.Trim(); target.Office = source.Office;
    }
    [HttpPost]
    public async Task<IActionResult> Create(Staff input)
    {
        if (!Valid(input)) return BadRequest("Name, staff number and valid office are required.");
        var staff = new Staff { Id = Guid.NewGuid(), CreatedBy = Owner };
        Copy(input, staff);
        db.Staff.Add(staff); await db.SaveChangesAsync();
        return Created($"/Staff/{staff.Id}", staff);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Staff input)
    {
        var staff = await db.Staff.SingleOrDefaultAsync(s => s.Id == id && s.CreatedBy == Owner);
        if (staff == null) return NotFound();
        if (input.Id != Guid.Empty && input.Id != id) return BadRequest("Route and body IDs differ.");
        if (!Valid(input)) return BadRequest("Name, staff number and valid office are required.");
        Copy(input, staff); await db.SaveChangesAsync(); return Ok(staff);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var staff = await db.Staff.SingleOrDefaultAsync(s => s.Id == id && s.CreatedBy == Owner);
        if (staff == null) return NotFound();
        if (await db.Reviews.AnyAsync(r => r.StaffId == id)) return Conflict("Delete the staff member's reviews first.");
        db.Staff.Remove(staff); await db.SaveChangesAsync(); return Ok(staff);
    }
}
