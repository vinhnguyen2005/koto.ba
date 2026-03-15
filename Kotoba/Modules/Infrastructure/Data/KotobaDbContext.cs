using Kotoba.Modules.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kotoba.Modules.Infrastructure.Data
{
    public class KotobaDbContext : IdentityDbContext<User>
    {
        public KotobaDbContext(DbContextOptions<KotobaDbContext> options)
            : base(options)
        {
        }

        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<MessageReceipt> MessageReceipts => Set<MessageReceipt>();
        public DbSet<Reaction> Reactions => Set<Reaction>();
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<Story> Stories => Set<Story>();
        public DbSet<CurrentThought> CurrentThoughts => Set<CurrentThought>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(u => u.DisplayName).HasMaxLength(120);
                entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            });

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("Conversations");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.GroupName).HasMaxLength(200);
                entity.Property(c => c.Type).HasConversion<string>().HasMaxLength(40);
            });

            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.ToTable("ConversationParticipants");
                entity.HasKey(cp => cp.Id);

                entity.HasOne(cp => cp.Conversation)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(cp => cp.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cp => cp.User)
                    .WithMany(u => u.ConversationParticipants)
                    .HasForeignKey(cp => cp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cp => new { cp.ConversationId, cp.UserId }).IsUnique();
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Messages");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Content).HasMaxLength(4000);

                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.Messages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MessageReceipt>(entity =>
            {
                entity.ToTable("MessageReceipts");
                entity.HasKey(mr => mr.Id);
                entity.Property(mr => mr.Status).HasConversion<string>().HasMaxLength(40);

                entity.HasOne(mr => mr.Message)
                    .WithMany(m => m.Receipts)
                    .HasForeignKey(mr => mr.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mr => mr.User)
                    .WithMany(u => u.MessageReceipts)
                    .HasForeignKey(mr => mr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(mr => new { mr.MessageId, mr.UserId }).IsUnique();
            });

            modelBuilder.Entity<Reaction>(entity =>
            {
                entity.ToTable("Reactions");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Type).HasConversion<string>().HasMaxLength(40);

                entity.HasOne(r => r.Message)
                    .WithMany(m => m.Reactions)
                    .HasForeignKey(r => r.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reactions)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();
            });

            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.ToTable("Attachments");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.FileName).HasMaxLength(260);
                entity.Property(a => a.FileUrl).HasMaxLength(1000);
                entity.Property(a => a.FileType).HasConversion<string>().HasMaxLength(40);

                entity.HasOne(a => a.Message)
                    .WithMany(m => m.Attachments)
                    .HasForeignKey(a => a.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Story>(entity =>
            {
                entity.ToTable("Stories");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Content).HasMaxLength(3000);
                entity.Property(s => s.MediaUrl).HasMaxLength(1000);

                entity.HasOne(s => s.User)
                    .WithMany(u => u.Stories)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CurrentThought>(entity =>
            {
                entity.ToTable("CurrentThoughts");
                entity.HasKey(ct => ct.Id);
                entity.Property(ct => ct.Content).HasMaxLength(1000);

                entity.HasOne(ct => ct.User)
                    .WithOne(u => u.CurrentThought)
                    .HasForeignKey<CurrentThought>(ct => ct.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ct => ct.UserId).IsUnique();
            });
        }
    }
}
