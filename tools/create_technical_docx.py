from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile
from xml.sax.saxutils import escape


OUT = Path("docs/docx")
OUT.mkdir(parents=True, exist_ok=True)


def x(text: object) -> str:
    return escape(str(text), {"'": "&apos;", '"': "&quot;"})


def p(text: str = "", style: str = "Normal") -> str:
    safe = x(text)
    return (
        f'<w:p><w:pPr><w:pStyle w:val="{style}"/></w:pPr>'
        f'<w:r><w:t xml:space="preserve">{safe}</w:t></w:r></w:p>'
    )


def table(headers: list[str], rows: list[list[str]], widths: list[int] | None = None) -> str:
    if widths is None:
        widths = [int(9360 / len(headers)) for _ in headers]
    grid = "".join(f'<w:gridCol w:w="{w}"/>' for w in widths)

    def cell(text: str, width: int, header: bool = False) -> str:
        shading = '<w:shd w:fill="F8F9FA"/>' if header else ""
        bold = "<w:b/>" if header else ""
        return (
            f'<w:tc><w:tcPr><w:tcW w:w="{width}" w:type="dxa"/>{shading}</w:tcPr>'
            f'<w:p><w:r><w:rPr>{bold}</w:rPr><w:t xml:space="preserve">{x(text)}</w:t></w:r></w:p></w:tc>'
        )

    body = [
        "<w:tr>"
        + "".join(cell(headers[i], widths[i], True) for i in range(len(headers)))
        + "</w:tr>"
    ]
    for row in rows:
        body.append(
            "<w:tr>"
            + "".join(cell(row[i] if i < len(row) else "", widths[i]) for i in range(len(headers)))
            + "</w:tr>"
        )

    borders = (
        '<w:tblBorders><w:top w:val="single" w:sz="4" w:color="DADCE0"/>'
        '<w:left w:val="single" w:sz="4" w:color="DADCE0"/>'
        '<w:bottom w:val="single" w:sz="4" w:color="DADCE0"/>'
        '<w:right w:val="single" w:sz="4" w:color="DADCE0"/>'
        '<w:insideH w:val="single" w:sz="4" w:color="DADCE0"/>'
        '<w:insideV w:val="single" w:sz="4" w:color="DADCE0"/></w:tblBorders>'
    )
    margins = (
        '<w:tblCellMar><w:top w:w="80" w:type="dxa"/><w:left w:w="120" w:type="dxa"/>'
        '<w:bottom w:w="80" w:type="dxa"/><w:right w:w="120" w:type="dxa"/></w:tblCellMar>'
    )
    return (
        '<w:tbl><w:tblPr><w:tblW w:w="9360" w:type="dxa"/>'
        f"{borders}{margins}</w:tblPr><w:tblGrid>{grid}</w:tblGrid>"
        + "".join(body)
        + "</w:tbl>"
    )


def doc_xml(elements: list[str]) -> str:
    section = (
        '<w:sectPr><w:pgSz w:w="12240" w:h="15840"/>'
        '<w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" '
        'w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>'
    )
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">'
        "<w:body>"
        + "".join(elements)
        + section
        + "</w:body></w:document>"
    )


STYLES = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:pPr><w:spacing w:after="160" w:line="276" w:lineRule="auto"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="22"/><w:color w:val="000000"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="DocTitle">
    <w:name w:val="Document Title"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="0" w:after="60"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="52"/><w:color w:val="000000"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Subtitle">
    <w:name w:val="Subtitle"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="0" w:after="240"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="22"/><w:color w:val="555555"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="Heading 1"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="400" w:after="120"/><w:outlineLvl w:val="0"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="40"/><w:color w:val="000000"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="Heading 2"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="360" w:after="120"/><w:outlineLvl w:val="1"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="32"/><w:color w:val="000000"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading3">
    <w:name w:val="Heading 3"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="320" w:after="80"/><w:outlineLvl w:val="2"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial"/><w:sz w:val="28"/><w:color w:val="434343"/></w:rPr>
  </w:style>
</w:styles>
"""


CONTENT_TYPES = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/word/settings.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
"""

RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
"""

DOC_RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings" Target="settings.xml"/>
</Relationships>
"""

SETTINGS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:settings xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:zoom w:percent="100"/>
  <w:defaultTabStop w:val="720"/>
</w:settings>
"""

APP = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"
 xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Astraea</Application>
</Properties>
"""


def core(title: str) -> str:
    return f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
 xmlns:dc="http://purl.org/dc/elements/1.1/"
 xmlns:dcterms="http://purl.org/dc/terms/"
 xmlns:dcmitype="http://purl.org/dc/dcmitype/"
 xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>{x(title)}</dc:title>
  <dc:creator>Astraea Project</dc:creator>
  <cp:lastModifiedBy>Astraea Project</cp:lastModifiedBy>
</cp:coreProperties>
"""


def write_docx(filename: str, title: str, elements: list[str]) -> Path:
    path = OUT / filename
    with ZipFile(path, "w", ZIP_DEFLATED) as z:
        z.writestr("[Content_Types].xml", CONTENT_TYPES)
        z.writestr("_rels/.rels", RELS)
        z.writestr("word/_rels/document.xml.rels", DOC_RELS)
        z.writestr("word/document.xml", doc_xml(elements))
        z.writestr("word/styles.xml", STYLES)
        z.writestr("word/settings.xml", SETTINGS)
        z.writestr("docProps/core.xml", core(title))
        z.writestr("docProps/app.xml", APP)
    return path


architecture = [
    p("Astraea System Architecture Overview", "DocTitle"),
    p("Technical Documentation - Google Docs-ready DOCX", "Subtitle"),
    p("Architectural Style", "Heading1"),
    p("Astraea uses a 4-layer Clean / N-Layer architecture. The frontend browser communicates with the Astraea.Web presentation layer, which depends on Astraea.Application contracts and delegates persistence, integration, and background work to Astraea.Infrastructure. Astraea.Domain remains the pure domain model layer."),
    table(
        ["Layer", "Responsibility", "Key Contents"],
        [
            ["Astraea.Domain", "Pure business model layer with no infrastructure dependencies.", "User, Skill, MentorLearner, StudyLog, PracticeSignal, GitHubConnection, Notification, RefresherContent, enums."],
            ["Astraea.Application", "Use-case contracts and DTO boundaries.", "DTOs, service interfaces, repository abstractions, Unit of Work abstraction, retention contracts."],
            ["Astraea.Infrastructure", "Persistence, external services, background jobs, and service implementations.", "AstraeaDbContext, DbInitializer, repositories, Unit of Work, GitHub sync, reports, reminders, nightly sync."],
            ["Astraea.Web", "Presentation host and API layer.", "Controllers, JWT auth, SignalR hub, static frontend, CORS, Serilog, app startup."],
        ],
        [1800, 3600, 3960],
    ),
    p("Authentication and Authorization", "Heading1"),
    p("The app uses JWT Bearer authentication. Tokens include identity claims and role claims. The supported roles are Learner, Mentor, Both, and Admin. Role-specific API endpoints use ASP.NET Core authorization attributes."),
    table(
        ["Role", "Primary privileges"],
        [
            ["Learner", "Manage skills, study logs, reports, GitHub connection, mentor access, reminders, and celestial map."],
            ["Mentor", "Accept/decline invitations, view mentees, open read-only dashboards, and send reminders."],
            ["Both", "Access both learner workflows and mentor workspace workflows."],
            ["Admin", "Represented in role model and token generation for future administrative workflows."],
        ],
        [1800, 7560],
    ),
    p("Main Workflows", "Heading1"),
    p("Learner skill tracking begins when a learner creates a skill with a constellation category and self-assessment. The system initializes ease factor, current interval, next review date, and canvas coordinates. Skill retention is then calculated from elapsed time and interval length."),
    p("Mentor access begins when a learner invites a mentor by email. Invitations are stored as pending. When accepted, the connection becomes read-only dashboard access. If a learner accepts another learner's invitation, the accepting account becomes Both."),
    p("GitHub integration uses OAuth to prove account ownership. After successful authorization, Astraea stores protected token data and can sync repository activity into practice signals."),
    p("Background Processing and Real-Time Updates", "Heading1"),
    p("NightlySyncBackgroundService performs scheduled maintenance such as GitHub syncing, retention status checks, and SignalR publishing. SignalR is exposed at /hubs/skill-status for live skill status updates."),
    p("Frontend Architecture", "Heading1"),
    p("The frontend is a single-page experience hosted in Astraea.Web/wwwroot/astraea-platform.html. It uses Tailwind CSS, HTML5 Canvas, JavaScript, Chart-based visualizations, role-aware navigation, and mobile-responsive hamburger navigation."),
]


api = [
    p("Astraea API Documentation", "DocTitle"),
    p("Technical Documentation - Google Docs-ready DOCX", "Subtitle"),
    p("API Overview", "Heading1"),
    p("Astraea exposes JSON APIs under /api/... and uses JWT Bearer authentication for protected routes. JSON uses camelCase naming. Local development normally runs at http://localhost:5000."),
    table(
        ["Area", "Route", "Authorization", "Purpose"],
        [
            ["Auth", "/api/auth/register", "Public", "Register a learner or mentor account."],
            ["Auth", "/api/auth/login", "Public", "Authenticate with email and password."],
            ["Auth", "/api/auth/change-password", "Authenticated", "Change current user's password."],
            ["GitHub Auth", "/api/auth/github/start", "Public", "Start GitHub sign-in OAuth."],
            ["GitHub Auth", "/api/auth/github/callback", "GitHub callback", "Complete GitHub sign-in or learner connection."],
            ["Skills", "/api/skills", "Learner", "List and create active learner skills."],
            ["Skills", "/api/skills/{id}/archive", "Learner", "Archive a skill into the Abyss."],
            ["Skills", "/api/skills/{id}/restore", "Learner", "Restore an archived skill."],
            ["Skills", "/api/skills/archived", "Learner", "List or clear archived skills."],
            ["Study Logs", "/api/studylogs", "Learner", "Record a study review."],
            ["Reports", "/api/reports", "Learner", "Return retention report data."],
            ["Learner Mentors", "/api/learner/mentors", "Learner", "List, invite, cancel, and revoke mentor access."],
            ["Learner Reminders", "/api/learner/reminders", "Learner", "List and mark mentor reminders as viewed."],
            ["Mentor Portal", "/api/mentor", "Mentor", "Mentor invitations, mentees, read-only dashboard, reminders."],
            ["Shared Mentor Invitations", "/api/mentor-invitations", "Learner, Mentor, Both", "Invitation inbox and mentee operations for Both-role flow."],
            ["GitHub", "/api/github", "Learner", "GitHub connection, OAuth start, sync, disconnect."],
            ["Refresher Content", "/api/refresher-content/skills/{skillId}", "Learner, Mentor", "Return refresher links for a skill."],
        ],
        [1600, 2500, 1700, 3560],
    ),
    p("Key DTOs", "Heading1"),
    table(
        ["DTO", "Used by", "Purpose"],
        [
            ["RegisterUserRequest", "Auth", "Full name, email, password, and selected role."],
            ["AuthResponseDto", "Auth and mentor accept", "JWT access token and user profile summary."],
            ["CreateSkillRequest", "Skills", "Skill creation payload including prerequisites."],
            ["CelestialNodeDto", "Skills and dashboards", "Canvas node, retention, status, and prerequisite ids."],
            ["MentorLearnerDto", "Mentor workflows", "Invitation and mentor connection representation."],
            ["LearnerSummaryDto", "Mentor dashboard", "Mentee summary statistics."],
            ["LearnerReportDto", "Reports", "Skill rows, review count, streak, and average retention change."],
            ["GitHubConnectionDto", "GitHub settings", "Connection state and sync metadata."],
        ],
        [2400, 2400, 4560],
    ),
    p("Representative Requests", "Heading1"),
    p("POST /api/auth/register accepts fullName, email, password, and role. It returns AuthResponseDto with accessToken, userId, fullName, and role."),
    p("POST /api/skills accepts title, constellationCategory, initialRating, targetWeeklyHours, and prerequisiteSkillIds. It returns the created CelestialNodeDto."),
    p("GET /api/reports returns skill retention rows with retentionPercent, thirtyDayChangePercent, status, reviewsThisMonth, longestStreakDays, and averageRetentionChangePercent."),
    p("POST /api/github/oauth/start returns a GitHub authorizationUrl. The callback must match the GitHub OAuth App redirect URL."),
    p("SignalR Hub", "Heading1"),
    p("The SignalR hub is available at /hubs/skill-status and is used for real-time skill status publishing."),
]


schema = [
    p("Astraea Database Schema", "DocTitle"),
    p("Technical Documentation - Google Docs-ready DOCX", "Subtitle"),
    p("Schema Overview", "Heading1"),
    p("Astraea uses Entity Framework Core 8 with SQL Server. The main DbContext is AstraeaDbContext. The schema centers on users, skills, mentor relationships, study activity, GitHub connections, notifications, and refresher content."),
    table(
        ["Table", "Primary purpose", "Key relationships"],
        [
            ["Users", "Application accounts and roles.", "One user owns many skills; can be learner or mentor in mentor relationships."],
            ["Skills", "Learner-tracked celestial map nodes.", "Belongs to Users; has study logs, practice signals, prerequisites, notifications, refresher content."],
            ["SkillPrerequisites", "Self-referencing prerequisite links between skills.", "Composite key SkillId + PrerequisiteSkillId."],
            ["MentorLearners", "Mentor invitations and accepted relationships.", "Links learner user to mentor email and optional mentor user."],
            ["StudyLogs", "Manual study/review sessions.", "Belongs to Skills."],
            ["PracticeSignals", "External practice events such as GitHub activity.", "Belongs to Skills."],
            ["GitHubConnections", "Learner GitHub OAuth and sync metadata.", "One connection per learner."],
            ["Notifications", "Mentor reminders and learner alerts.", "Links learner, mentor, and skill."],
            ["RefresherContents", "Resource links for skill refreshers.", "Belongs to Skills."],
        ],
        [1900, 3300, 4160],
    ),
    p("Important Tables", "Heading1"),
    table(
        ["Table", "Important columns"],
        [
            ["Users", "Id, Email, PasswordHash, FullName, Role, CreatedAtUtc."],
            ["Skills", "Id, LearnerId, Title, ConstellationCategory, SelfAssessedRating, TargetWeeklyHours, EaseFactor, CurrentIntervalDays, LastReviewedUtc, NextReviewDueDateUtc, CanvasX, CanvasY, IsArchived."],
            ["MentorLearners", "Id, LearnerId, MentorEmail, MentorUserId, Status, InvitedAtUtc, StatusUpdatedAtUtc."],
            ["GitHubConnections", "Id, LearnerId, GitHubUsername, GitHubUserId, IsOAuthVerified, AccessTokenProtected, OAuthState, OAuthStateExpiresUtc, ConnectedAtUtc, LastSyncDateUtc, LastReposScanned, LastSignalsImported."],
            ["Notifications", "Id, LearnerId, MentorId, SkillId, Message, IsRead, CreatedAtUtc, ReadAtUtc."],
        ],
        [2200, 7160],
    ),
    p("Relationships", "Heading1"),
    table(
        ["Relationship", "Type", "Delete behavior / notes"],
        [
            ["Users to Skills", "One-to-many", "Skill has LearnerId foreign key."],
            ["Users to MentorLearners as learner", "One-to-many", "Restricted delete."],
            ["Users to MentorLearners as mentor", "One-to-many optional", "MentorUserId is nullable; restricted delete."],
            ["Skills to SkillPrerequisites", "Many-to-many self-reference", "Composite key; restricted delete."],
            ["Skills to StudyLogs", "One-to-many", "Indexed by SkillId and StudiedAtUtc."],
            ["Skills to PracticeSignals", "One-to-many", "Indexed by SkillId and OccurredAtUtc."],
            ["Users to GitHubConnections", "One-to-one logical relationship", "Unique LearnerId; cascade delete."],
            ["Skills to RefresherContents", "One-to-many", "Cascade delete."],
        ],
        [3100, 2200, 4060],
    ),
    p("Indexes and Constraints", "Heading1"),
    table(
        ["Object", "Constraint or index", "Reason"],
        [
            ["Users", "Unique Email", "Fast login lookup and duplicate-account prevention."],
            ["MentorLearners", "Unique LearnerId + MentorEmail", "Prevents duplicate invites for the same learner and mentor email."],
            ["GitHubConnections", "Unique LearnerId", "Ensures one GitHub connection per learner."],
            ["GitHubConnections", "Index OAuthState", "Fast OAuth callback lookup."],
            ["StudyLogs", "Index SkillId + StudiedAtUtc", "Efficient review timeline and report queries."],
            ["PracticeSignals", "Index SkillId + OccurredAtUtc", "Efficient external activity timeline queries."],
            ["Notifications", "Index LearnerId + IsRead + CreatedAtUtc", "Efficient unread reminder loading."],
        ],
        [2300, 3300, 3760],
    ),
    p("Enum Values", "Heading1"),
    table(
        ["Enum", "Values"],
        [
            ["UserRole", "0 Learner; 1 Mentor; 2 Both; 3 Admin."],
            ["MentorLearnerStatus", "0 Pending; 1 Accepted; 2 Declined; 3 Revoked."],
            ["RiskStatus", "0 Fresh; 1 Fading; 2 AtRisk."],
            ["PracticeSource", "0 Manual; 1 GitHub."],
        ],
        [2600, 6760],
    ),
]


write_docx("Astraea-System-Architecture-Overview.docx", "Astraea System Architecture Overview", architecture)
write_docx("Astraea-API-Documentation.docx", "Astraea API Documentation", api)
write_docx("Astraea-Database-Schema.docx", "Astraea Database Schema", schema)
print("created")
for path in sorted(OUT.glob("*.docx")):
    print(path)
