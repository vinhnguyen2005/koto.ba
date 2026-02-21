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

    // TODO: Add DbSet properties for each subsystem
    // Example:
    // public DbSet<Conversation> Conversations { get; set; }
    // public DbSet<Message> Messages { get; set; }
    // public DbSet<Reaction> Reactions { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Reaction> Reactions { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.ToTable("Message");

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(m => m.Content).IsRequired();
            entity.HasQueryFilter(m => !m.IsDeleted);
        });

        builder.Entity<Reaction>()
            .HasOne(r => r.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
        });
        // TODO: Add entity configurations for each subsystem
        // Example:
        // builder.ApplyConfiguration(new MessageConfiguration());
    }
}
