using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<PracticeSession> PracticeSessions { get; set; }

        public DbSet<CustomTask> CustomTasks { get; set; }
        public DbSet<PunctuationTask> PunctuationTasks { get; set; }
        public DbSet<SpellingTask> SpellingTasks { get; set; }

        public DbSet<AnswerOption> AnswerOptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User relationships
            modelBuilder.Entity<User>()
                .HasMany(u => u.Achievements)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.PracticeSessions)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<CustomTask>()
                .HasDiscriminator<string>("TaskType")
                .HasValue<CustomTask>("Custom")
                .HasValue<PunctuationTask>("Punctuation")
                .HasValue<SpellingTask>("Spelling");

            modelBuilder.Entity<AnswerOption>()
                .HasOne(to => to.CustomTask)
                .WithMany(ct => ct.AnswerOptions)
                .HasForeignKey(to => to.CustomTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PunctuationTask>()
                .Ignore(pt => pt.Highlights);
        }
    }
}
