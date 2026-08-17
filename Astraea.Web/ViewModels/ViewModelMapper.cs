using Astraea.Application;

namespace Astraea.Web.ViewModels;

public static class ViewModelMapper
{
    public static AuthResponseVm ToVm(this AuthResponseDto dto)
    {
        return new AuthResponseVm(dto.AccessToken, dto.UserId, dto.FullName, dto.Role);
    }

    public static MentorLearnerVm ToVm(this MentorLearnerDto dto)
    {
        return new MentorLearnerVm(
            dto.Id,
            dto.MentorEmail,
            dto.MentorName,
            dto.Status,
            dto.InvitedAtUtc,
            dto.StatusUpdatedAtUtc);
    }

    public static LearnerSummaryVm ToVm(this LearnerSummaryDto dto)
    {
        return new LearnerSummaryVm(
            dto.Id,
            dto.FullName,
            dto.Email,
            dto.SkillCount,
            dto.AverageRetention,
            dto.LastActiveUtc);
    }

    public static CelestialNodeVm ToVm(this CelestialNodeDto dto)
    {
        return new CelestialNodeVm(
            dto.Id,
            dto.Title,
            dto.ConstellationCategory,
            dto.Rating,
            dto.TargetWeeklyHours,
            dto.EaseFactor,
            dto.CurrentIntervalDays,
            dto.LastReviewedUtc,
            dto.NextReviewDueDateUtc,
            dto.CanvasX,
            dto.CanvasY,
            dto.RetentionPercent,
            dto.RiskStatus,
            dto.PrerequisiteSkillIds);
    }

    public static LearnerDashboardVm ToVm(this LearnerDashboardDto dto)
    {
        return new LearnerDashboardVm(
            dto.Learner.ToVm(),
            dto.Nodes.Select(x => x.ToVm()).ToArray());
    }

    public static SkillReportRowVm ToVm(this SkillReportRowDto dto)
    {
        return new SkillReportRowVm(
            dto.Skill,
            dto.Category,
            dto.RetentionPercent,
            dto.ThirtyDayChangePercent,
            dto.Status);
    }

    public static LearnerReportVm ToVm(this LearnerReportDto dto)
    {
        return new LearnerReportVm(
            dto.Skills.Select(x => x.ToVm()).ToArray(),
            dto.ReviewsThisMonth,
            dto.LongestStreakDays,
            dto.AverageRetentionChangePercent);
    }

    public static MentorReminderVm ToVm(this MentorReminderDto dto)
    {
        return new MentorReminderVm(
            dto.Id,
            dto.MentorName,
            dto.SkillTitle,
            dto.Message,
            dto.CreatedAtUtc);
    }

    public static RefresherContentVm ToVm(this RefresherContentDto dto)
    {
        return new RefresherContentVm(
            dto.Id,
            dto.SkillId,
            dto.Title,
            dto.Url,
            dto.Provider);
    }

    public static GitHubConnectionVm ToVm(this GitHubConnectionDto dto)
    {
        return new GitHubConnectionVm(
            dto.IsConnected,
            dto.GitHubUsername,
            dto.ConnectedAtUtc,
            dto.LastSyncDateUtc,
            dto.ReposScanned,
            dto.SignalsImported,
            dto.IsVerified);
    }

    public static GitHubSyncResultVm ToVm(this GitHubSyncResultDto dto)
    {
        return new GitHubSyncResultVm(
            dto.GitHubUsername,
            dto.ReposScanned,
            dto.SignalsImported,
            dto.SyncedAtUtc);
    }

    public static GitHubOAuthStartVm ToVm(this GitHubOAuthStartDto dto)
    {
        return new GitHubOAuthStartVm(dto.AuthorizationUrl);
    }
}
