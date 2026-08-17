using Astraea.Domain;
using Microsoft.EntityFrameworkCore;
namespace Astraea.Infrastructure;

public static class DbInitializer
{
    private static readonly (string Title, string Category, double Ease, int DaysSinceReview)[] SeedSkills =
    [
        ("JavaScript","Web Engineering",2.5,1), ("React","Web Engineering",2.2,4), ("Node.js","Web Engineering",1.8,12),
        ("C#","Web Engineering",2.5,2), ("ASP.NET Core","Web Engineering",2.0,8), ("Python","Data Science",2.5,1),
        ("SQL","Data Science",2.1,6), ("Machine Learning","Data Science",1.7,16)
    ];
    public static async Task InitializeAsync(AstraeaDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (db.Database.IsSqlServer())
        {
            await db.Database.ExecuteSqlRawAsync("IF OBJECT_ID(N'[Notifications]', N'U') IS NULL CREATE TABLE [Notifications]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[LearnerId] uniqueidentifier NOT NULL,[MentorId] uniqueidentifier NOT NULL,[SkillId] uniqueidentifier NOT NULL,[Message] nvarchar(max) NOT NULL,[IsRead] bit NOT NULL,[CreatedAtUtc] datetime2 NOT NULL,[ReadAtUtc] datetime2 NULL)");
            await db.Database.ExecuteSqlRawAsync("IF OBJECT_ID(N'[RefresherContents]', N'U') IS NULL CREATE TABLE [RefresherContents]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[SkillId] uniqueidentifier NOT NULL,[Title] nvarchar(220) NOT NULL,[Url] nvarchar(600) NOT NULL,[Provider] nvarchar(80) NOT NULL,[CreatedAtUtc] datetime2 NOT NULL)");
            await db.Database.ExecuteSqlRawAsync("IF OBJECT_ID(N'[GitHubConnections]', N'U') IS NULL CREATE TABLE [GitHubConnections]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[LearnerId] uniqueidentifier NOT NULL,[GitHubUsername] nvarchar(100) NOT NULL,[GitHubUserId] bigint NULL,[IsOAuthVerified] bit NOT NULL DEFAULT 0,[AccessTokenProtected] nvarchar(max) NULL,[OAuthState] nvarchar(120) NULL,[OAuthStateExpiresUtc] datetime2 NULL,[ConnectedAtUtc] datetime2 NOT NULL,[LastSyncDateUtc] datetime2 NULL,[LastReposScanned] int NOT NULL,[LastSignalsImported] int NOT NULL)");
            await db.Database.ExecuteSqlRawAsync("IF COL_LENGTH('GitHubConnections','GitHubUserId') IS NULL ALTER TABLE [GitHubConnections] ADD [GitHubUserId] bigint NULL");
            await db.Database.ExecuteSqlRawAsync("IF COL_LENGTH('GitHubConnections','IsOAuthVerified') IS NULL ALTER TABLE [GitHubConnections] ADD [IsOAuthVerified] bit NOT NULL CONSTRAINT DF_GitHubConnections_IsOAuthVerified DEFAULT 0");
            await db.Database.ExecuteSqlRawAsync("IF COL_LENGTH('GitHubConnections','AccessTokenProtected') IS NULL ALTER TABLE [GitHubConnections] ADD [AccessTokenProtected] nvarchar(max) NULL");
            await db.Database.ExecuteSqlRawAsync("IF COL_LENGTH('GitHubConnections','OAuthState') IS NULL ALTER TABLE [GitHubConnections] ADD [OAuthState] nvarchar(120) NULL");
            await db.Database.ExecuteSqlRawAsync("IF COL_LENGTH('GitHubConnections','OAuthStateExpiresUtc') IS NULL ALTER TABLE [GitHubConnections] ADD [OAuthStateExpiresUtc] datetime2 NULL");
        }
        var learner = await db.Users.Include(x => x.Skills).SingleOrDefaultAsync(x => x.Email == "learner@astraea.io");
        if (learner is null)
        {
            learner = new User { Email = "learner@astraea.io", FullName = "Astraea Learner", Role = UserRole.Learner, PasswordHash = Hash("Astraea!123") };
            var mentor = new User { Email = "mentor@astraea.io", FullName = "Astraea Mentor", Role = UserRole.Mentor, PasswordHash = Hash("Astraea!123") };
            db.Users.AddRange(learner, mentor);
            foreach (var (item, index) in SeedSkills.Select((item, index) => (item, index))) db.Skills.Add(new Skill { LearnerId = learner.Id, Title = item.Title, ConstellationCategory = item.Category, EaseFactor = item.Ease, CurrentIntervalDays = 7, SelfAssessedRating = 4, TargetWeeklyHours = 4, LastReviewedUtc = DateTime.UtcNow.AddDays(-item.DaysSinceReview), NextReviewDueDateUtc = DateTime.UtcNow.AddDays(7 - item.DaysSinceReview), CanvasX = .2 + (index % 4) * .2, CanvasY = .25 + (index / 4) * .4 });
            db.MentorLearners.Add(new MentorLearner { LearnerId = learner.Id, MentorEmail = mentor.Email, MentorUserId = mentor.Id, Status = MentorLearnerStatus.Accepted });
            await db.SaveChangesAsync(); learner = await db.Users.Include(x => x.Skills).SingleAsync(x => x.Email == "learner@astraea.io");
        }
        await ApplySeedProfileAsync(db, learner);
        await RemoveSeededMentorRelationshipAsync(db);
        await SeedGitHubConnectionAsync(db, learner);
    }
    private static async Task ApplySeedProfileAsync(AstraeaDbContext db, User learner)
    {
        var skills = await db.Skills.Where(x => x.LearnerId == learner.Id && SeedSkills.Select(s => s.Title).Contains(x.Title)).ToListAsync();
        if (skills.Count != SeedSkills.Length) return;
        var now = DateTime.UtcNow;
        foreach (var spec in SeedSkills) { var skill = skills.Single(x => x.Title == spec.Title); skill.ConstellationCategory = spec.Category; skill.EaseFactor = spec.Ease; skill.CurrentIntervalDays = 7; skill.LastReviewedUtc = now.AddDays(-spec.DaysSinceReview); skill.NextReviewDueDateUtc = now.AddDays(7 - spec.DaysSinceReview); }
        var ids = skills.Select(x => x.Id).ToHashSet();
        db.SkillPrerequisites.RemoveRange(db.SkillPrerequisites.Where(x => ids.Contains(x.SkillId) && ids.Contains(x.PrerequisiteSkillId)));
        await db.SaveChangesAsync();
        db.SkillPrerequisites.AddRange(new SkillPrerequisite { SkillId = skills.Single(x => x.Title == "React").Id, PrerequisiteSkillId = skills.Single(x => x.Title == "JavaScript").Id }, new SkillPrerequisite { SkillId = skills.Single(x => x.Title == "ASP.NET Core").Id, PrerequisiteSkillId = skills.Single(x => x.Title == "C#").Id }, new SkillPrerequisite { SkillId = skills.Single(x => x.Title == "Machine Learning").Id, PrerequisiteSkillId = skills.Single(x => x.Title == "Python").Id });
        await db.SaveChangesAsync();
        await SeedRefresherContentAsync(db, skills);
    }
    private static async Task SeedRefresherContentAsync(AstraeaDbContext db, IReadOnlyCollection<Skill> skills)
    {
        var seedSkillIds = skills.Select(x => x.Id).ToArray();
        if (await db.RefresherContents.AnyAsync(x => seedSkillIds.Contains(x.SkillId))) return;
        db.RefresherContents.AddRange(skills.SelectMany(skill => new[]
        {
            new RefresherContent{SkillId=skill.Id,Title=$"{skill.Title} refresher for self taught learners",Url=YouTubeSearch(skill.Title+" refresher self taught learners")},
            new RefresherContent{SkillId=skill.Id,Title=$"{skill.Title} practice review",Url=YouTubeSearch(skill.Title+" practice review tutorial")}
        }));
        await db.SaveChangesAsync();
    }
    private static async Task SeedGitHubConnectionAsync(AstraeaDbContext db, User learner)
    {
        if (await db.GitHubConnections.AnyAsync(x => x.LearnerId == learner.Id)) return;
        db.GitHubConnections.Add(new GitHubConnection { LearnerId = learner.Id, GitHubUsername = "astraea-demo", IsOAuthVerified = false, ConnectedAtUtc = DateTime.UtcNow.AddDays(-7), LastSyncDateUtc = DateTime.UtcNow.AddHours(-6), LastReposScanned = 8, LastSignalsImported = 4 });
        await db.SaveChangesAsync();
    }
    private static async Task RemoveSeededMentorRelationshipAsync(AstraeaDbContext db)
    {
        var seededLearner = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Email == "learner@astraea.io");
        if (seededLearner is null) return;
        var seededRelationships = await db.MentorLearners.Where(x => x.LearnerId == seededLearner.Id && x.MentorEmail == "mentor@astraea.io").ToListAsync();
        if (seededRelationships.Count == 0) return;
        db.MentorLearners.RemoveRange(seededRelationships);
        await db.SaveChangesAsync();
    }
    private static string YouTubeSearch(string query) => "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query);
    private static string Hash(string p) { var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16); var key = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(p, salt, 210000, System.Security.Cryptography.HashAlgorithmName.SHA512, 32); return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(key); }
}
