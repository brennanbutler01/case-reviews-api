using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using case_reviews_server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace case_reviews_server;

public static class VisitorDemo
{
    public const string Issuer = "case-reviews-visitor-demo";

    public static void MapVisitorDemo(this WebApplication app, string signingKey)
    {
        app.MapPost("/demo/session", async (ApplicationDbContext db) =>
        {
            var session = new VisitorSession
            {
                Id = $"visitor-{Guid.NewGuid():N}",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            db.VisitorSessions.Add(session);
            await db.SaveChangesAsync();
            var token = new JwtSecurityToken(Issuer, Issuer,
                [new Claim("sub", session.Id)], expires: session.ExpiresAt,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256));
            return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), subject = session.Id, expiresAt = session.ExpiresAt });
        }).RequireRateLimiting("sessions");
        app.MapGet("/demo/session", (ClaimsPrincipal user) => Results.Ok(new { subject = user.FindFirst("sub")!.Value }))
            .RequireAuthorization();
        app.MapDelete("/demo/session", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            await DeleteSession(db, user.FindFirst("sub")!.Value);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    public static async Task DeleteSession(ApplicationDbContext db, string subject)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serialize reset with in-flight writes so revoked sessions cannot leave new records behind.
        var session = await db.VisitorSessions.FromSqlInterpolated(
            $"SELECT * FROM \"VisitorSessions\" WHERE \"Id\" = {subject} FOR UPDATE").SingleOrDefaultAsync();
        if (session == null) return;
        var reviews = await db.Reviews.Where(r => r.ReviewedBy == subject)
            .Include(r => r.ReviewElements).Include(r => r.MagiEligibles).ToListAsync();
        foreach (var review in reviews)
        {
            db.ReviewElements.RemoveRange(review.ReviewElements);
            if (review.MagiEligibles != null) db.MagiEligibles.Remove(review.MagiEligibles);
        }
        db.Reviews.RemoveRange(reviews);
        db.Staff.RemoveRange(await db.Staff.Where(s => s.CreatedBy == subject).ToListAsync());
        db.VisitorSessions.RemoveRange(await db.VisitorSessions.Where(s => s.Id == subject).ToListAsync());
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}

public sealed class VisitorCleanup(IServiceScopeFactory scopes, ILogger<VisitorCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var expired = await db.VisitorSessions.Where(s => s.ExpiresAt <= DateTime.UtcNow)
                    .Select(s => s.Id).ToListAsync(stoppingToken);
                foreach (var subject in expired) await VisitorDemo.DeleteSession(db, subject);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Visitor data cleanup failed; retrying on the next interval"); }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
