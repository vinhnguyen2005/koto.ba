using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // TODO: Add DbSet properties for each subsystem
    // Example:
    // public DbSet<Conversation> Conversations { get; set; }
    // public DbSet<Message> Messages { get; set; }
    // public DbSet<Reaction> Reactions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // TODO: Add entity configurations for each subsystem
        // Example:
        // builder.ApplyConfiguration(new MessageConfiguration());
    }
}
