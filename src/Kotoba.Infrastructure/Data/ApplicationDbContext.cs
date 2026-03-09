using Kotoba.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Reaction> Reactions { get; set; }
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Story> Stories { get; set; }
    public DbSet<CurrentThought> CurrentThoughts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure table names to match the migration (singular)
        builder.Entity<Conversation>().ToTable("Conversation");
        builder.Entity<Message>().ToTable("Message");
        builder.Entity<Reaction>().ToTable("Reaction");
        builder.Entity<ConversationParticipant>().ToTable("ConversationParticipant");
        builder.Entity<Attachment>().ToTable("Attachment");
        builder.Entity<Story>().ToTable("Story");
        builder.Entity<CurrentThought>().ToTable("CurrentThought");

        builder.Entity<Reaction>()
            .HasOne(r => r.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.NoAction);

        // TODO: Add entity configurations for each subsystem
        // Example:
        // builder.ApplyConfiguration(new MessageConfiguration());
    }
}
