using Astraea.Domain;
namespace Astraea.Application;
public record RegisterUserRequest(string FullName,string Email,string Password,UserRole Role);
public record LoginRequest(string Email,string Password);
public record AuthResponseDto(string AccessToken,Guid UserId,string FullName,string Role);
public record ChangePasswordRequest(string CurrentPassword,string NewPassword);
public record ForgotPasswordResetRequest(string Email,string NewPassword);
public record MentorInviteRequest(string Email);
public record MentorLearnerDto(Guid Id,string MentorEmail,string? MentorName,MentorLearnerStatus Status,DateTime InvitedAtUtc,DateTime StatusUpdatedAtUtc);
public record LearnerSummaryDto(Guid Id,string FullName,string Email,int SkillCount,double AverageRetention,DateTime LastActiveUtc);
public record CreateSkillRequest(string Title,string ConstellationCategory,int InitialRating,decimal TargetWeeklyHours,IReadOnlyCollection<Guid>? PrerequisiteSkillIds);
public record CelestialNodeDto(Guid Id,string Title,string ConstellationCategory,int Rating,double EaseFactor,int CurrentIntervalDays,double CanvasX,double CanvasY,double RetentionPercent,RiskStatus RiskStatus,IReadOnlyCollection<Guid> PrerequisiteSkillIds);
public record LearnerDashboardDto(LearnerSummaryDto Learner,IReadOnlyCollection<CelestialNodeDto> Nodes);
public record SkillReportRowDto(string Skill,string Category,double RetentionPercent,double ThirtyDayChangePercent,string Status);
public record LearnerReportDto(IReadOnlyCollection<SkillReportRowDto> Skills,int ReviewsThisMonth,int LongestStreakDays,double AverageRetentionChangePercent);
public record MentorReminderDto(Guid Id,string MentorName,string SkillTitle,string Message,DateTime CreatedAtUtc);
public record RefresherContentDto(Guid Id,Guid SkillId,string Title,string Url,string Provider);
public record GitHubConnectRequest(string Username);
public record GitHubConnectionDto(bool IsConnected,string? GitHubUsername,DateTime? ConnectedAtUtc,DateTime? LastSyncDateUtc,int ReposScanned,int SignalsImported,bool IsVerified);
public record GitHubSyncResultDto(string GitHubUsername,int ReposScanned,int SignalsImported,DateTime SyncedAtUtc);
public record GitHubOAuthStartDto(string AuthorizationUrl);
public interface IAuthService { Task<AuthResponseDto> RegisterAsync(RegisterUserRequest request,CancellationToken ct); Task<AuthResponseDto> LoginAsync(LoginRequest request,CancellationToken ct); Task ChangePasswordAsync(Guid userId,ChangePasswordRequest request,CancellationToken ct); Task ResetForgottenPasswordAsync(ForgotPasswordResetRequest request,CancellationToken ct); }
public interface IMentorService { Task<IReadOnlyCollection<MentorLearnerDto>> GetLearnerMentorsAsync(Guid learnerId,CancellationToken ct); Task<MentorLearnerDto> InviteMentorAsync(Guid learnerId,string email,CancellationToken ct); Task CancelInvitationAsync(Guid id,Guid learnerId,CancellationToken ct); Task RevokeAccessAsync(Guid id,Guid learnerId,CancellationToken ct); Task<IReadOnlyCollection<MentorLearnerDto>> GetPendingInvitationsForMentorAsync(Guid mentorId,CancellationToken ct); Task<AuthResponseDto> AcceptInvitationAsync(Guid id,Guid mentorId,CancellationToken ct); Task DeclineInvitationAsync(Guid id,Guid mentorId,CancellationToken ct); Task<IReadOnlyCollection<LearnerSummaryDto>> GetActiveMenteesAsync(Guid mentorId,CancellationToken ct); Task<LearnerDashboardDto> GetLearnerDashboardReadOnlyAsync(Guid mentorId,Guid learnerId,CancellationToken ct); }
public interface ISkillService { Task<CelestialNodeDto> CreateSkillAsync(Guid learnerId,CreateSkillRequest request,CancellationToken ct); Task<IReadOnlyCollection<CelestialNodeDto>> GetSkillsAsync(Guid learnerId,CancellationToken ct); }
public interface IReportService { Task<LearnerReportDto> GetLearnerReportAsync(Guid learnerId,CancellationToken ct); }
public interface IReminderService { Task SendFadingSkillReminderAsync(Guid mentorId,Guid learnerId,CancellationToken ct); Task<IReadOnlyCollection<MentorReminderDto>> GetUnreadForLearnerAsync(Guid learnerId,CancellationToken ct); Task MarkReadAsync(Guid learnerId,Guid notificationId,CancellationToken ct); }
public interface IGitHubSyncService { Task<GitHubConnectionDto> GetConnectionAsync(Guid learnerId,CancellationToken ct); Task<GitHubConnectionDto> ConnectAsync(Guid learnerId,GitHubConnectRequest request,CancellationToken ct); Task<GitHubOAuthStartDto> BeginOAuthAsync(Guid learnerId,string callbackUrl,CancellationToken ct); Task CompleteOAuthAsync(string code,string state,CancellationToken ct); Task DisconnectAsync(Guid learnerId,CancellationToken ct); Task<GitHubSyncResultDto> SyncAsync(Guid learnerId,CancellationToken ct); }
public interface ITokenFactory { string Create(User user); }
