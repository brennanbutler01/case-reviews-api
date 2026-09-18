using case_reviews_server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace case_reviews_server;

public sealed class VisitorWriteFilter(ApplicationDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method is not ("POST" or "PUT" or "DELETE"))
        {
            await next(); return;
        }
        var subject = context.HttpContext.User.FindFirst("sub")?.Value;
        await using var transaction = await db.Database.BeginTransactionAsync();
        var session = await db.VisitorSessions.FromSqlInterpolated(
            $"SELECT * FROM \"VisitorSessions\" WHERE \"Id\" = {subject} FOR UPDATE").SingleOrDefaultAsync();
        if (session == null || session.ExpiresAt <= DateTime.UtcNow)
        {
            context.Result = new UnauthorizedResult(); return;
        }
        var executed = await next();
        var status = (executed.Result as IStatusCodeActionResult)?.StatusCode ?? 200;
        // Commit before serializing the action result so a successful response means the write persisted.
        if (executed.Exception == null && status < 400) await transaction.CommitAsync();
    }
}
