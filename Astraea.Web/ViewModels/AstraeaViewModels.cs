using Astraea.Domain;

namespace Astraea.Web.ViewModels;

public record AuthResponseVm(
    string AccessToken,
    Guid UserId,
    string FullName,
    string Role);

public record MentorLearnerVm(
    Guid Id,
    string MentorEmail,
    string? MentorName,
    MentorLearnerStatus Status,
    DateTime InvitedAtUtc,
    DateTime StatusUpdatedAtUtc);

public record LearnerSummaryVm(
    Guid Id,
    string FullName,
    string Email,
    int SkillCount,
    double AverageRetention,
    DateTime LastActiveUtc);

public record CelestialNodeVm(
    Guid Id,
    string Title,
    string ConstellationCategory,
    int Rating,
    decimal TargetWeeklyHours,
    double EaseFactor,
    int CurrentIntervalDays,
    DateTime LastReviewedUtc,
    DateTime NextReviewDueDateUtc,
    double CanvasX,
    double CanvasY,
    double RetentionPercent,
    RiskStatus RiskStatus,
    IReadOnlyCollection<Guid> PrerequisiteSkillIds);

public record LearnerDashboardVm(
    LearnerSummaryVm Learner,
    IReadOnlyCollection<CelestialNodeVm> Nodes);

public record SkillReportRowVm(
    string Skill,
    string Category,
    double RetentionPercent,
    double ThirtyDayChangePercent,
    string Status);

public record LearnerReportVm(
    IReadOnlyCollection<SkillReportRowVm> Skills,
    int ReviewsThisMonth,
    int LongestStreakDays,
    double AverageRetentionChangePercent);

public record MentorReminderVm(
    Guid Id,
    string MentorName,
    string SkillTitle,
    string Message,
    DateTime CreatedAtUtc);

public record RefresherContentVm(
    Guid Id,
    Guid SkillId,
    string Title,
    string Url,
    string Provider);

public record GitHubConnectionVm(
    bool IsConnected,
    string? GitHubUsername,
    DateTime? ConnectedAtUtc,
    DateTime? LastSyncDateUtc,
    int ReposScanned,
    int SignalsImported,
    bool IsVerified);

public record GitHubSyncResultVm(
    string GitHubUsername,
    int ReposScanned,
    int SignalsImported,
    DateTime SyncedAtUtc);

public record GitHubOAuthStartVm(string AuthorizationUrl);

public record ArchivedSkillVm(
    Guid Id,
    string Title,
    string ConstellationCategory,
    double EaseFactor,
    int CurrentIntervalDays,
    double CanvasX,
    double CanvasY,
    double RetentionPercent);
