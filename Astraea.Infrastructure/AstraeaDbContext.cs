using Astraea.Domain;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Infrastructure;

public sealed class AstraeaDbContext(DbContextOptions<AstraeaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<MentorLearner> MentorLearners => Set<MentorLearner>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillPrerequisite> SkillPrerequisites => Set<SkillPrerequisite>();
    public DbSet<StudyLog> StudyLogs => Set<StudyLog>();
    public DbSet<PracticeSignal> PracticeSignals => Set<PracticeSignal>();
    public DbSet<GitHubConnection> GitHubConnections => Set<GitHubConnection>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefresherContent> RefresherContents => Set<RefresherContent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(320);
        });

        builder.Entity<MentorLearner>(entity =>
        {
            entity.HasIndex(x => new { x.LearnerId, x.MentorEmail }).IsUnique();

            entity
                .HasOne(x => x.Learner)
                .WithMany()
                .HasForeignKey(x => x.LearnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.MentorUser)
                .WithMany()
                .HasForeignKey(x => x.MentorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Skill>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(160);

            entity
                .HasOne(x => x.Learner)
                .WithMany(x => x.Skills)
                .HasForeignKey(x => x.LearnerId);
        });

        builder
            .Entity<SkillPrerequisite>()
            .HasKey(x => new { x.SkillId, x.PrerequisiteSkillId });

        builder
            .Entity<SkillPrerequisite>()
            .HasOne(x => x.Skill)
            .WithMany(x => x.Prerequisites)
            .HasForeignKey(x => x.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Entity<SkillPrerequisite>()
            .HasOne(x => x.PrerequisiteSkill)
            .WithMany()
            .HasForeignKey(x => x.PrerequisiteSkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudyLog>(entity =>
        {
            entity.HasIndex(x => new { x.SkillId, x.StudiedAtUtc });

            entity
                .HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId);
        });

        builder.Entity<PracticeSignal>(entity =>
        {
            entity.HasIndex(x => new { x.SkillId, x.OccurredAtUtc });

            entity
                .HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId);
        });

        builder.Entity<GitHubConnection>(entity =>
        {
            entity.HasIndex(x => x.LearnerId).IsUnique();
            entity.HasIndex(x => x.OAuthState);
            entity.Property(x => x.GitHubUsername).HasMaxLength(100);
            entity.Property(x => x.OAuthState).HasMaxLength(120);

            entity
                .HasOne(x => x.Learner)
                .WithMany()
                .HasForeignKey(x => x.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(x => new { x.LearnerId, x.IsRead, x.CreatedAtUtc });

            entity
                .HasOne(x => x.Learner)
                .WithMany()
                .HasForeignKey(x => x.LearnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.Mentor)
                .WithMany()
                .HasForeignKey(x => x.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefresherContent>(entity =>
        {
            entity.HasIndex(x => x.SkillId);
            entity.Property(x => x.Title).HasMaxLength(220);
            entity.Property(x => x.Url).HasMaxLength(600);
            entity.Property(x => x.Provider).HasMaxLength(80);

            entity
                .HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
