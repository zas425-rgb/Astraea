using Astraea.Application;
using Astraea.Application.Abstractions;
using Astraea.Domain;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Infrastructure;

public sealed class MentorService(AstraeaDbContext db, IUnitOfWork uow, ITokenFactory tokens) : IMentorService
{
    public async Task<MentorLearnerDto> InviteMentorAsync(Guid learnerId, string email, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Enter a mentor email first.");
        }

        var learner = await db.Users.FindAsync([learnerId], ct) ?? throw new KeyNotFoundException("Learner not found.");
        if (learner.Email == email)
        {
            throw new InvalidOperationException("You cannot invite yourself.");
        }

        var invitedUser = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (invitedUser is null)
        {
            throw new InvalidOperationException("No Astraea user exists with that email.");
        }

        var existing = await db.MentorLearners
            .Include(x => x.MentorUser)
            .SingleOrDefaultAsync(x => x.LearnerId == learnerId && x.MentorEmail == email, ct);

        if (existing is not null)
        {
            if (existing.Status is MentorLearnerStatus.Pending or MentorLearnerStatus.Accepted)
            {
                throw new InvalidOperationException("An active invitation already exists.");
            }

            existing.Status = MentorLearnerStatus.Pending;
            existing.MentorUserId = invitedUser?.Id;
            existing.StatusUpdatedAtUtc = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
            return ToLearnerMentorDto(existing, invitedUser);
        }

        var item = new MentorLearner
        {
            LearnerId = learnerId,
            MentorEmail = email,
            MentorUserId = invitedUser?.Id,
            Status = MentorLearnerStatus.Pending,
            InvitedAtUtc = DateTime.UtcNow,
            StatusUpdatedAtUtc = DateTime.UtcNow
        };

        db.MentorLearners.Add(item);
        await uow.SaveChangesAsync(ct);
        return ToLearnerMentorDto(item, invitedUser);
    }

    public async Task<IReadOnlyCollection<MentorLearnerDto>> GetLearnerMentorsAsync(Guid learnerId, CancellationToken ct)
    {
        return await db.MentorLearners
            .AsNoTracking()
            .Where(x => x.LearnerId == learnerId && (x.Status == MentorLearnerStatus.Pending || x.Status == MentorLearnerStatus.Accepted || x.Status == MentorLearnerStatus.Declined))
            .Include(x => x.MentorUser)
            .OrderByDescending(x => x.StatusUpdatedAtUtc)
            .Select(x => new MentorLearnerDto(x.Id, x.MentorEmail, x.MentorUser == null ? null : x.MentorUser.FullName, x.Status, x.InvitedAtUtc, x.StatusUpdatedAtUtc))
            .ToListAsync(ct);
    }

    public Task CancelInvitationAsync(Guid id, Guid learnerId, CancellationToken ct)
    {
        return SetLearnerStatusAsync(id, learnerId, MentorLearnerStatus.Pending, MentorLearnerStatus.Revoked, ct);
    }

    public Task RevokeAccessAsync(Guid id, Guid learnerId, CancellationToken ct)
    {
        return SetLearnerStatusAsync(id, learnerId, MentorLearnerStatus.Accepted, MentorLearnerStatus.Revoked, ct);
    }

    public async Task<IReadOnlyCollection<MentorLearnerDto>> GetPendingInvitationsForMentorAsync(Guid mentorId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([mentorId], ct) ?? throw new KeyNotFoundException("User not found.");
        return await db.MentorLearners
            .AsNoTracking()
            .Where(x => x.MentorEmail == user.Email && x.Status == MentorLearnerStatus.Pending)
            .Include(x => x.Learner)
            .OrderByDescending(x => x.InvitedAtUtc)
            .Select(x => new MentorLearnerDto(x.Id, x.Learner.Email, x.Learner.FullName, x.Status, x.InvitedAtUtc, x.StatusUpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<AuthResponseDto> AcceptInvitationAsync(Guid id, Guid mentorId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([mentorId], ct) ?? throw new KeyNotFoundException("User not found.");
        var invitation = await db.MentorLearners
            .SingleOrDefaultAsync(x => x.Id == id && x.MentorEmail == user.Email && x.Status == MentorLearnerStatus.Pending, ct)
            ?? throw new KeyNotFoundException("Invitation not found.");

        invitation.Status = MentorLearnerStatus.Accepted;
        invitation.MentorUserId = mentorId;
        invitation.StatusUpdatedAtUtc = DateTime.UtcNow;
        if (user.Role == UserRole.Learner)
        {
            user.Role = UserRole.Both;
        }

        await uow.SaveChangesAsync(ct);
        return new AuthResponseDto(tokens.Create(user), user.Id, user.FullName, user.Role.ToString());
    }

    public async Task DeclineInvitationAsync(Guid id, Guid mentorId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([mentorId], ct) ?? throw new KeyNotFoundException("User not found.");
        var invitation = await db.MentorLearners
            .SingleOrDefaultAsync(x => x.Id == id && x.MentorEmail == user.Email && x.Status == MentorLearnerStatus.Pending, ct)
            ?? throw new KeyNotFoundException("Invitation not found.");

        invitation.Status = MentorLearnerStatus.Declined;
        invitation.StatusUpdatedAtUtc = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<LearnerSummaryDto>> GetActiveMenteesAsync(Guid mentorId, CancellationToken ct)
    {
        var mentor = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mentorId, ct) ?? throw new KeyNotFoundException("User not found.");
        var rows = await db.MentorLearners
            .AsNoTracking()
            .Where(x => x.Status == MentorLearnerStatus.Accepted && (x.MentorUserId == mentorId || x.MentorEmail == mentor.Email))
            .Include(x => x.Learner)
            .ThenInclude(x => x.Skills)
            .ToListAsync(ct);

        return rows.Select(x =>
        {
            var nodes = x.Learner.Skills.Where(s => !s.IsArchived).Select(SkillService.Map).ToArray();
            return new LearnerSummaryDto(
                x.LearnerId,
                x.Learner.FullName,
                x.Learner.Email,
                nodes.Length,
                nodes.Length == 0 ? 0 : Math.Round(nodes.Average(n => n.RetentionPercent), 1),
                x.StatusUpdatedAtUtc);
        }).ToArray();
    }

    public async Task<LearnerDashboardDto> GetLearnerDashboardReadOnlyAsync(Guid mentorId, Guid learnerId, CancellationToken ct)
    {
        var mentor = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mentorId, ct) ?? throw new KeyNotFoundException("User not found.");
        var canView = await db.MentorLearners
            .AsNoTracking()
            .AnyAsync(x => x.LearnerId == learnerId && x.Status == MentorLearnerStatus.Accepted && (x.MentorUserId == mentorId || x.MentorEmail == mentor.Email), ct);
        if (!canView)
        {
            throw new UnauthorizedAccessException("You do not mentor this learner.");
        }

        var learner = await db.Users
            .AsNoTracking()
            .Include(x => x.Skills)
            .ThenInclude(x => x.Prerequisites)
            .SingleAsync(x => x.Id == learnerId, ct);
        var nodes = learner.Skills.Where(x => !x.IsArchived).Select(SkillService.Map).ToArray();
        return new LearnerDashboardDto(
            new LearnerSummaryDto(learner.Id, learner.FullName, learner.Email, nodes.Length, nodes.Length == 0 ? 0 : Math.Round(nodes.Average(n => n.RetentionPercent), 1), learner.CreatedAtUtc),
            nodes);
    }

    private async Task SetLearnerStatusAsync(Guid id, Guid learnerId, MentorLearnerStatus expected, MentorLearnerStatus next, CancellationToken ct)
    {
        var invitation = await db.MentorLearners
            .SingleOrDefaultAsync(x => x.Id == id && x.LearnerId == learnerId && x.Status == expected, ct)
            ?? throw new KeyNotFoundException("Invitation not found.");

        invitation.Status = next;
        invitation.StatusUpdatedAtUtc = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);
    }

    private static MentorLearnerDto ToLearnerMentorDto(MentorLearner item, User? invitedUser)
    {
        return new MentorLearnerDto(item.Id, item.MentorEmail, invitedUser?.FullName, item.Status, item.InvitedAtUtc, item.StatusUpdatedAtUtc);
    }
}
