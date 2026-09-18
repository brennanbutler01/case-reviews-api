using Microsoft.EntityFrameworkCore;

namespace case_reviews_server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<VisitorSession> VisitorSessions { get; set; }

    public DbSet<Staff> Staff { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ReviewElement> ReviewElements { get; set; }
    public DbSet<MagiEligibles> MagiEligibles { get; set; }
}