namespace Astraea.Domain;

public enum UserRole
{
    Learner = 0,
    Mentor = 1,
    Both = 2,
    Admin = 3
}

public enum MentorLearnerStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Revoked = 3
}

public enum RiskStatus
{
    Fresh,
    Fading,
    AtRisk
}

public enum PracticeSource
{
    Manual,
    GitHub
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
}

public sealed class MentorLearner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearnerId { get; set; }
    public User Learner { get; set; } = null!;
    public string MentorEmail { get; set; } = "";
    public Guid? MentorUserId { get; set; }
    public User? MentorUser { get; set; }
    public MentorLearnerStatus Status { get; set; } = MentorLearnerStatus.Pending;
    public DateTime InvitedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime StatusUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Skill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearnerId { get; set; }
    public User Learner { get; set; } = null!;
    public string Title { get; set; } = "";
    public string ConstellationCategory { get; set; } = "";
    public int SelfAssessedRating { get; set; }
    public decimal TargetWeeklyHours { get; set; }
    public double EaseFactor { get; set; } = 2.5;
    public int CurrentIntervalDays { get; set; } = 1;
    public DateTime LastReviewedUtc { get; set; } = DateTime.UtcNow;
    public DateTime NextReviewDueDateUtc { get; set; } = DateTime.UtcNow.AddDays(1);
    public double CanvasX { get; set; }
    public double CanvasY { get; set; }
    public bool IsArchived { get; set; }
    public ICollection<SkillPrerequisite> Prerequisites { get; set; } = new List<SkillPrerequisite>();
}

public sealed class SkillPrerequisite
{
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public Guid PrerequisiteSkillId { get; set; }
    public Skill PrerequisiteSkill { get; set; } = null!;
}

public sealed class StudyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public DateTime StudiedAtUtc { get; set; } = DateTime.UtcNow;
    public int SelfRating { get; set; }
    public string Notes { get; set; } = "";
    public PracticeSource Source { get; set; } = PracticeSource.Manual;
}

public sealed class PracticeSignal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public PracticeSource Source { get; set; }
    public string ExternalReference { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
    public decimal Hours { get; set; }
}

public sealed class GitHubConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearnerId { get; set; }
    public User Learner { get; set; } = null!;
    public string GitHubUsername { get; set; } = "";
    public long? GitHubUserId { get; set; }
    public bool IsOAuthVerified { get; set; }
    public string? AccessTokenProtected { get; set; }
    public string? OAuthState { get; set; }
    public DateTime? OAuthStateExpiresUtc { get; set; }
    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncDateUtc { get; set; }
    public int LastReposScanned { get; set; }
    public int LastSignalsImported { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearnerId { get; set; }
    public User Learner { get; set; } = null!;
    public Guid MentorId { get; set; }
    public User Mentor { get; set; } = null!;
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}

public sealed class RefresherContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Provider { get; set; } = "YouTube";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
