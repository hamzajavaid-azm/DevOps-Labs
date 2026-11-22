using Microsoft.EntityFrameworkCore;

namespace Lab_01.Data;

// The DbContext is EF Core's unit of work + the gateway to the database.
// One DbSet maps to one table.
public class AppDbContext : DbContext
{
    // The options (which provider, which connection string) are injected by DI.
    // They are configured in Program.cs via AddDbContext.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Query/insert tasks through this property, e.g. db.Tasks.ToListAsync().
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}
