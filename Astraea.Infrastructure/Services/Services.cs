using System.Security.Cryptography;
using Astraea.Application;
using Astraea.Application.Abstractions;
using Astraea.Domain;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Infrastructure;

public sealed class AuthService(
    AstraeaDbContext db,
    IUnitOfWork uow,
    ITokenFactory tokens) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(x => x.Email == email, ct))
        {
            throw new InvalidOperationException("An account already uses this email.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = request.Role,
            PasswordHash = Hash(request.Password)
        };

        db.Users.Add(user);

        if (request.Role == UserRole.Mentor)
        {
            var pendingInvites = await db.MentorLearners
                .Where(x => x.MentorEmail == email && x.Status == MentorLearnerStatus.Pending)
                .ToListAsync(ct);

            foreach (var invite in pendingInvites)
            {
                invite.MentorUserId = user.Id;
            }
        }

        await uow.SaveChangesAsync(ct);
        return new AuthResponseDto(tokens.Create(user), user.Id, user.FullName, user.Role.ToString());
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return new AuthResponseDto(tokens.Create(user), user.Id, user.FullName, user.Role.ToString());
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new InvalidOperationException("Your new password must be at least eight characters.");
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new UnauthorizedAccessException();

        if (!Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Your current password is incorrect.");
        }

        user.PasswordHash = Hash(request.NewPassword);
        await uow.SaveChangesAsync(ct);
    }

    public async Task ResetForgottenPasswordAsync(ForgotPasswordResetRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Enter the account email first.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new InvalidOperationException("Your new password must be at least eight characters.");
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct)
            ?? throw new InvalidOperationException("No Astraea user exists with that email.");

        user.PasswordHash = Hash(request.NewPassword);
        await uow.SaveChangesAsync(ct);
    }

    private static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            210000,
            HashAlgorithmName.SHA512,
            32);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(key);
    }

    private static bool Verify(string password, string hash)
    {
        var parts = hash.Split(':');

        if (parts.Length != 2)
        {
            return false;
        }

        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            Convert.FromBase64String(parts[0]),
            210000,
            HashAlgorithmName.SHA512,
            32);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public sealed class SkillService(
    AstraeaDbContext db,
    IUnitOfWork uow) : ISkillService
{
    public async Task<CelestialNodeDto> CreateSkillAsync(
        Guid learnerId,
        CreateSkillRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.ConstellationCategory)
            || request.InitialRating is < 1 or > 5)
        {
            throw new InvalidOperationException(
                "A title, constellation category, and one to five stars are required.");
        }

        var (easeFactor, intervalDays) = request.InitialRating <= 2
            ? (1.7, 1)
            : request.InitialRating == 3
                ? (2.0, 3)
                : (2.5, 7);

        var skillCount = await db.Skills.CountAsync(x => x.LearnerId == learnerId, ct);
        var angle = skillCount * 2.399963229728653;

        var skill = new Skill
        {
            LearnerId = learnerId,
            Title = request.Title.Trim(),
            ConstellationCategory = request.ConstellationCategory.Trim(),
            SelfAssessedRating = request.InitialRating,
            TargetWeeklyHours = request.TargetWeeklyHours,
            EaseFactor = easeFactor,
            CurrentIntervalDays = intervalDays,
            LastReviewedUtc = DateTime.UtcNow,
            NextReviewDueDateUtc = DateTime.UtcNow.AddDays(intervalDays),
            CanvasX = 0.5 + Math.Cos(angle) * 0.30,
            CanvasY = 0.5 + Math.Sin(angle) * 0.30
        };

        foreach (var prerequisiteId in request.PrerequisiteSkillIds ?? [])
        {
            skill.Prerequisites.Add(new SkillPrerequisite
            {
                SkillId = skill.Id,
                PrerequisiteSkillId = prerequisiteId
            });
        }

        db.Skills.Add(skill);
        await uow.SaveChangesAsync(ct);

        return Map(skill);
    }

    public async Task<IReadOnlyCollection<CelestialNodeDto>> GetSkillsAsync(
        Guid learnerId,
        CancellationToken ct)
    {
        var skills = await db.Skills
            .AsNoTracking()
            .Include(x => x.Prerequisites)
            .Where(x => x.LearnerId == learnerId && !x.IsArchived)
            .OrderBy(x => x.Title)
            .ToListAsync(ct);

        return skills.Select(Map).ToArray();
    }

    internal static CelestialNodeDto Map(Skill skill)
    {
        var elapsedDays = (DateTime.UtcNow - skill.LastReviewedUtc).TotalDays;
        var retention = Math.Clamp(
            100 * Math.Exp(-elapsedDays / Math.Max(skill.CurrentIntervalDays, 1)),
            0,
            100);

        var riskStatus = retention >= 70
            ? RiskStatus.Fresh
            : retention >= 40
                ? RiskStatus.Fading
                : RiskStatus.AtRisk;

        return new CelestialNodeDto(
            skill.Id,
            skill.Title,
            skill.ConstellationCategory,
            skill.SelfAssessedRating,
            skill.TargetWeeklyHours,
            skill.EaseFactor,
            skill.CurrentIntervalDays,
            skill.LastReviewedUtc,
            skill.NextReviewDueDateUtc,
            skill.CanvasX,
            skill.CanvasY,
            retention,
            riskStatus,
            skill.Prerequisites.Select(x => x.PrerequisiteSkillId).ToArray());
    }
}
