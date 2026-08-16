from __future__ import annotations

from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile
from xml.sax.saxutils import escape


OUT = Path("presentation")
OUT.mkdir(exist_ok=True)
FINAL = OUT / "Astraea-System-Presentation.pptx"

SLIDE_W = 12192000
SLIDE_H = 6858000
EMU = 9525

COLORS = {
    "bg": "050711",
    "bg2": "0B0E1E",
    "panel": "0F1226",
    "lav": "C7D2FE",
    "muted": "94A3B8",
    "teal": "00F5D4",
    "amber": "FFB703",
    "coral": "E2574C",
    "indigo": "6366F1",
    "white": "F8FAFC",
}


def e(text: object) -> str:
    return escape(str(text), {"'": "&apos;", '"': "&quot;"})


def px(value: float) -> int:
    return int(round(value * EMU))


def color_xml(hex_color: str, alpha: int | None = None) -> str:
    alpha_xml = f'<a:alpha val="{alpha}"/>' if alpha is not None else ""
    return f'<a:srgbClr val="{hex_color}">{alpha_xml}</a:srgbClr>'


def solid_fill(hex_color: str, alpha: int | None = None) -> str:
    return f"<a:solidFill>{color_xml(hex_color, alpha)}</a:solidFill>"


def line_xml(hex_color: str = "FFFFFF", width: int = 1, alpha: int | None = None) -> str:
    return f'<a:ln w="{max(1, width) * 12700}">{solid_fill(hex_color, alpha)}</a:ln>'


def no_line() -> str:
    return "<a:ln><a:noFill/></a:ln>"


def shape(
    sid: int,
    name: str,
    x: float,
    y: float,
    w: float,
    h: float,
    fill: str,
    geom: str = "roundRect",
    alpha: int | None = None,
    line: str | None = None,
) -> str:
    line = line if line is not None else line_xml("C7D2FE", 1, 20000)
    return f"""
    <p:sp>
      <p:nvSpPr><p:cNvPr id="{sid}" name="{e(name)}"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
      <p:spPr>
        <a:xfrm><a:off x="{px(x)}" y="{px(y)}"/><a:ext cx="{px(w)}" cy="{px(h)}"/></a:xfrm>
        <a:prstGeom prst="{geom}"><a:avLst/></a:prstGeom>
        {solid_fill(fill, alpha)}
        {line}
      </p:spPr>
    </p:sp>"""


def line_shape(sid: int, name: str, x1: float, y1: float, x2: float, y2: float, color: str, width: int = 2) -> str:
    left, top = min(x1, x2), min(y1, y2)
    w, h = abs(x2 - x1), abs(y2 - y1)
    flip_h = ' flipH="1"' if x2 < x1 else ""
    flip_v = ' flipV="1"' if y2 < y1 else ""
    return f"""
    <p:cxnSp>
      <p:nvCxnSpPr><p:cNvPr id="{sid}" name="{e(name)}"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr>
      <p:spPr>
        <a:xfrm{flip_h}{flip_v}><a:off x="{px(left)}" y="{px(top)}"/><a:ext cx="{px(max(w, 1))}" cy="{px(max(h, 1))}"/></a:xfrm>
        <a:prstGeom prst="line"><a:avLst/></a:prstGeom>
        {line_xml(color, width)}
      </p:spPr>
    </p:cxnSp>"""


def textbox(
    sid: int,
    name: str,
    text: str,
    x: float,
    y: float,
    w: float,
    h: float,
    size: int = 24,
    color: str = "C7D2FE",
    bold: bool = False,
    align: str = "l",
) -> str:
    b = ' b="1"' if bold else ""
    paras = text.split("\n")
    para_xml = ""
    for para in paras:
        para_xml += (
            f'<a:p><a:pPr algn="{align}"/>'
            f'<a:r><a:rPr lang="en-US" sz="{size * 100}"{b}>'
            f'{solid_fill(color)}<a:latin typeface="Aptos"/></a:rPr><a:t>{e(para)}</a:t></a:r>'
            f'<a:endParaRPr sz="{size * 100}"/></a:p>'
        )
    return f"""
    <p:sp>
      <p:nvSpPr><p:cNvPr id="{sid}" name="{e(name)}"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
      <p:spPr>
        <a:xfrm><a:off x="{px(x)}" y="{px(y)}"/><a:ext cx="{px(w)}" cy="{px(h)}"/></a:xfrm>
        <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
        <a:noFill/>{no_line()}
      </p:spPr>
      <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0"/><a:lstStyle/>{para_xml}</p:txBody>
    </p:sp>"""


def bullet_textbox(sid: int, name: str, items: list[str], x: float, y: float, w: float, h: float, size: int = 18) -> str:
    paras = ""
    for item in items:
        paras += (
            '<a:p><a:pPr marL="342900" indent="-228600">'
            '<a:buChar char="•"/></a:pPr>'
            f'<a:r><a:rPr lang="en-US" sz="{size * 100}">{solid_fill(COLORS["lav"])}<a:latin typeface="Aptos"/></a:rPr>'
            f'<a:t>{e(item)}</a:t></a:r><a:endParaRPr sz="{size * 100}"/></a:p>'
        )
    return f"""
    <p:sp>
      <p:nvSpPr><p:cNvPr id="{sid}" name="{e(name)}"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
      <p:spPr>
        <a:xfrm><a:off x="{px(x)}" y="{px(y)}"/><a:ext cx="{px(w)}" cy="{px(h)}"/></a:xfrm>
        <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
        <a:noFill/>{no_line()}
      </p:spPr>
      <p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0"/><a:lstStyle/>{paras}</p:txBody>
    </p:sp>"""


def starfield(start_id: int = 500) -> tuple[str, int]:
    points = [(54, 88), (184, 620), (248, 104), (352, 548), (476, 72), (612, 638), (742, 126), (922, 596), (1068, 92), (1188, 642), (1210, 224), (86, 404), (1012, 360), (710, 508)]
    xml = ""
    sid = start_id
    for i, (x, y) in enumerate(points):
        color = COLORS["teal"] if i % 4 == 0 else COLORS["lav"]
        alpha = 85000 if i % 4 == 0 else 45000
        size = 3 if i % 3 else 4
        xml += shape(sid, f"star-{i}", x, y, size, size, color, "ellipse", alpha, no_line())
        sid += 1
    return xml, sid


def slide_xml(slide_elements: list[str]) -> str:
    stars, _ = starfield()
    return f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"
       xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"
       xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld>
    <p:bg><p:bgPr>{solid_fill(COLORS["bg"])}<a:effectLst/></p:bgPr></p:bg>
    <p:spTree>
      <p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>
      <p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{SLIDE_W}" cy="{SLIDE_H}"/><a:chOff x="0" y="0"/><a:chExt cx="{SLIDE_W}" cy="{SLIDE_H}"/></a:xfrm></p:grpSpPr>
      {shape(2, "nebula-top", -90, -120, 520, 360, COLORS["indigo"], "ellipse", 18000, no_line())}
      {shape(3, "nebula-right", 930, 80, 460, 460, COLORS["teal"], "ellipse", 10000, no_line())}
      {stars}
      {''.join(slide_elements)}
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sld>"""


def title_bar(title: str, idx: int, subtitle: str | None = None) -> list[str]:
    elements = [
        textbox(20, "slide-number", f"{idx:02d}", 1138, 42, 70, 30, 14, COLORS["muted"], True, "r"),
        textbox(21, "eyebrow", "ASTRAEA", 72, 42, 160, 30, 14, COLORS["teal"], True),
        textbox(22, "title", title, 72, 84, 820, 64, 36, COLORS["lav"], True),
    ]
    if subtitle:
        elements.append(textbox(23, "subtitle", subtitle, 74, 146, 760, 34, 16, COLORS["muted"]))
    return elements


def card(sid: int, x: float, y: float, w: float, h: float, heading: str, body: str, accent: str = "teal") -> list[str]:
    color = COLORS[accent]
    return [
        shape(sid, heading + " card", x, y, w, h, COLORS["panel"], "roundRect", 78000, line_xml("C7D2FE", 1, 18000)),
        shape(sid + 1, heading + " dot", x + 22, y + 24, 10, 10, color, "ellipse", None, no_line()),
        textbox(sid + 2, heading + " heading", heading, x + 42, y + 16, w - 64, 30, 20, color, True),
        textbox(sid + 3, heading + " body", body, x + 24, y + 58, w - 48, h - 72, 16, COLORS["lav"]),
    ]


slides: list[list[str]] = []

slides.append([
    shape(10, "hero-glass", 76, 108, 760, 430, COLORS["panel"], "roundRect", 76000, line_xml("C7D2FE", 1, 20000)),
    textbox(11, "brand", "ASTRAEA", 116, 142, 360, 48, 20, COLORS["teal"], True),
    textbox(12, "main-title", "Celestial Skill\n& Retention Tracker", 116, 208, 650, 150, 50, COLORS["lav"], True),
    textbox(13, "subtitle", "A full-stack ASP.NET Core 8 system for mapping learned skills, predicting memory decay, and enabling read-only mentor guidance.", 120, 378, 620, 80, 20, COLORS["muted"]),
    shape(14, "teal-orbit", 900, 164, 210, 210, COLORS["teal"], "ellipse", 16000, line_xml("00F5D4", 2, 80000)),
    shape(15, "amber-star", 986, 242, 38, 38, COLORS["amber"], "ellipse", None, no_line()),
    shape(16, "coral-star", 1076, 360, 24, 24, COLORS["coral"], "ellipse", None, no_line()),
    line_shape(17, "constellation-line-1", 1005, 261, 1088, 371, COLORS["lav"], 1),
    line_shape(18, "constellation-line-2", 1005, 261, 914, 188, COLORS["lav"], 1),
    textbox(19, "footer", "Feature overview • Architecture • Challenges", 118, 598, 680, 28, 16, COLORS["muted"]),
])

slides.append(title_bar("What Astraea Solves", 2, "Self-taught learning fades quietly unless practice, review, and mentorship become visible.") + [
    *card(30, 72, 220, 340, 190, "Memory decay is invisible", "Astraea turns elapsed time and review quality into clear retention states: Fresh, Fading, and At Risk.", "coral"),
    *card(40, 470, 220, 340, 190, "Practice is fragmented", "Manual study logs and GitHub practice signals are combined into one learner-owned skill timeline.", "amber"),
    *card(50, 868, 220, 340, 190, "Mentoring needs boundaries", "Mentors can observe progress and send reminders, but all learner data remains read-only.", "teal"),
])

slides.append(title_bar("Feature Suite", 3, "Astraea covers the complete learner loop from skill creation to reminders and reports.") + [
    *card(60, 72, 210, 265, 145, "Celestial Map", "Animated HTML5 Canvas stars grouped by constellation category.", "teal"),
    *card(70, 367, 210, 265, 145, "Retention Engine", "SM-2-style ease factor and interval updates power review scheduling.", "amber"),
    *card(80, 662, 210, 265, 145, "Reports", "Retention table, trend graph, bar chart, monthly reviews, streaks, and CSV export.", "teal"),
    *card(90, 957, 210, 265, 145, "The Abyss", "Archived skills persist until the learner explicitly clears them.", "coral"),
    *card(100, 220, 405, 330, 135, "GitHub Integration", "OAuth verifies ownership; repository activity becomes practice signals.", "teal"),
    *card(110, 730, 405, 330, 135, "Mentor Workspace", "Invitation inbox, mentee list, observer view, and fading-skill reminders.", "amber"),
])

slides.append(title_bar("Learner Journey", 4, "The learner experience is centered on ownership, visibility, and review momentum.") + [
    line_shape(120, "flowline", 168, 364, 1112, 364, COLORS["teal"], 3),
    *card(121, 72, 260, 215, 175, "1. Register", "Learner signs up, logs in, and receives a role-based JWT.", "teal"),
    *card(131, 320, 260, 215, 175, "2. Add Skills", "Rating initializes ease factor and review interval.", "amber"),
    *card(141, 568, 260, 215, 175, "3. Review", "Study logs update the decay curve and next review date.", "teal"),
    *card(151, 816, 260, 215, 175, "4. Improve", "Reports, refreshers, GitHub sync, and mentor nudges keep skills lit.", "coral"),
])

slides.append(title_bar("Mentor & Both-Role Workflow", 5, "Mentors get read-only visibility; learners can become Both when invited by another learner.") + [
    *card(160, 78, 220, 315, 150, "Invite", "A learner invites a mentor by email. The connection is stored as Pending.", "teal"),
    *card(170, 482, 220, 315, 150, "Accept / Decline", "The invited user receives an inbox notification. Accepting grants observer access.", "amber"),
    *card(180, 886, 220, 315, 150, "Observe", "Mentor opens a read-only dashboard and can send skill reminders.", "teal"),
    line_shape(190, "invite-arrow-1", 394, 295, 482, 295, COLORS["lav"], 2),
    line_shape(191, "invite-arrow-2", 797, 295, 886, 295, COLORS["lav"], 2),
    shape(192, "read-only-banner", 250, 488, 780, 58, COLORS["panel"], "roundRect", 76000, line_xml("00F5D4", 1, 50000)),
    textbox(193, "read-only-text", "READ-ONLY PRINCIPLE: mentors can view progress and send reminders, but they cannot edit learner skills, logs, or settings.", 284, 506, 720, 28, 18, COLORS["teal"], True, "c"),
])

slides.append(title_bar("System Architecture", 6, "Four strict layers keep business rules, persistence, and presentation cleanly separated.") + [
    shape(200, "web-layer", 165, 190, 950, 72, COLORS["panel"], "roundRect", 79000, line_xml("00F5D4", 2, 70000)),
    textbox(201, "web-text", "Astraea.Web  |  Controllers, JWT, SignalR, Serilog, Static SPA", 205, 212, 870, 28, 22, COLORS["teal"], True, "c"),
    shape(202, "app-layer", 205, 294, 870, 72, COLORS["panel"], "roundRect", 79000, line_xml("C7D2FE", 1, 30000)),
    textbox(203, "app-text", "Astraea.Application  |  DTOs, Interfaces, Retention Contracts, UoW Abstractions", 235, 316, 810, 28, 20, COLORS["lav"], True, "c"),
    shape(204, "infra-layer", 245, 398, 790, 72, COLORS["panel"], "roundRect", 79000, line_xml("FFB703", 2, 70000)),
    textbox(205, "infra-text", "Astraea.Infrastructure  |  EF Core, Services, Repositories, GitHub, Background Jobs", 275, 420, 730, 28, 20, COLORS["amber"], True, "c"),
    shape(206, "domain-layer", 285, 502, 710, 72, COLORS["panel"], "roundRect", 79000, line_xml("E2574C", 2, 70000)),
    textbox(207, "domain-text", "Astraea.Domain  |  Pure Entities and Enums", 315, 524, 650, 28, 20, COLORS["coral"], True, "c"),
    line_shape(208, "dep1", 640, 262, 640, 294, COLORS["lav"], 2),
    line_shape(209, "dep2", 640, 366, 640, 398, COLORS["lav"], 2),
    line_shape(210, "dep3", 640, 470, 640, 502, COLORS["lav"], 2),
])

slides.append(title_bar("Database Model", 7, "The schema centers on learners, skills, mentor relationships, and practice evidence.") + [
    *card(220, 72, 205, 260, 135, "Users", "Identity, password hash, full name, role, created date.", "teal"),
    *card(230, 510, 205, 260, 135, "Skills", "Retention parameters, canvas coordinates, archive state.", "amber"),
    *card(240, 948, 205, 260, 135, "MentorLearners", "Pending, accepted, declined, revoked access records.", "teal"),
    *card(250, 72, 430, 260, 120, "StudyLogs", "Manual reviews with self-rating and notes.", "coral"),
    *card(260, 510, 430, 260, 120, "GitHubConnections", "OAuth verified username, protected token, sync stats.", "teal"),
    *card(270, 948, 430, 260, 120, "Notifications", "Mentor reminders linked to learner, mentor, and skill.", "amber"),
    line_shape(280, "u-s", 332, 270, 510, 270, COLORS["lav"], 2),
    line_shape(281, "s-m", 770, 270, 948, 270, COLORS["lav"], 2),
    line_shape(282, "s-log", 640, 340, 202, 430, COLORS["lav"], 2),
    line_shape(283, "s-gh", 640, 340, 640, 430, COLORS["lav"], 2),
    line_shape(284, "s-notif", 640, 340, 1078, 430, COLORS["lav"], 2),
])

slides.append(title_bar("Integrations, Realtime, and Automation", 8, "External practice and real-time updates make the map feel alive.") + [
    *card(290, 85, 230, 320, 185, "GitHub OAuth", "Learners connect their real GitHub account. OAuth verifies ownership before sync.", "teal"),
    *card(300, 480, 230, 320, 185, "Nightly Sync", "Background service checks connected learners, imports signals, and recalculates retention.", "amber"),
    *card(310, 875, 230, 320, 185, "SignalR Broadcast", "Skill status updates can be pushed to connected clients without a full refresh.", "coral"),
    line_shape(320, "integrate-1", 405, 320, 480, 320, COLORS["lav"], 2),
    line_shape(321, "integrate-2", 800, 320, 875, 320, COLORS["lav"], 2),
    textbox(322, "refresher", "Refresher content uses skill-aware YouTube search links so fading skills have immediate review resources.", 180, 502, 920, 48, 20, COLORS["lav"], False, "c"),
])

slides.append(title_bar("Engineering Challenges", 9, "The hard parts were mostly about correctness, ownership, and persistence.") + [
    table_like := shape(330, "challenge-frame", 76, 190, 1128, 390, COLORS["panel"], "roundRect", 76000, line_xml("C7D2FE", 1, 18000)),
    textbox(331, "c1", "Challenge", 110, 220, 230, 32, 22, COLORS["teal"], True),
    textbox(332, "s1", "Solution", 510, 220, 230, 32, 22, COLORS["teal"], True),
    line_shape(333, "rule", 100, 270, 1180, 270, COLORS["lav"], 1),
    textbox(334, "challenge-rows", "Role complexity\n\nPersistence after refresh\n\nGitHub ownership\n\nReadable responsive UI", 110, 300, 330, 220, 19, COLORS["lav"], True),
    textbox(335, "solution-rows", "Learner, Mentor, Both, and role-scoped controllers\n\nEF Core storage for skills, prerequisites, abyss, reminders, and GitHub data\n\nOAuth flow instead of trusting typed usernames\n\nHamburger navigation, glass panels, and canvas resize handling", 510, 300, 600, 220, 19, COLORS["muted"]),
])

slides.append(title_bar("Submission Readiness", 10, "Astraea now covers the required backend, frontend, and extra-credit expectations except AI, which was intentionally skipped.") + [
    *card(340, 72, 215, 345, 160, "Core Requirements", "4 layers, repository pattern, Unit of Work, authentication, roles, APIs, validation, tests, dashboards, responsive theme.", "teal"),
    *card(350, 468, 215, 345, 160, "Extra Features", "GitHub external API, SignalR realtime updates, background service, Serilog logging, CSV export, mentor reminders.", "amber"),
    *card(360, 864, 215, 345, 160, "Demo Narrative", "Register, add a skill, view decay, invite a mentor, accept, observe read-only, send reminder, sync GitHub.", "coral"),
    textbox(370, "closing", "Map your mind. Never lose a learned skill.", 190, 505, 900, 64, 38, COLORS["teal"], True, "c"),
])


def write_package() -> None:
    n = len(slides)
    content_overrides = [
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>',
        '<Default Extension="xml" ContentType="application/xml"/>',
        '<Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/>',
        '<Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/>',
        '<Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>',
        '<Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>',
        '<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>',
        '<Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>',
    ] + [
        f'<Override PartName="/ppt/slides/slide{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>'
        for i in range(1, n + 1)
    ]
    content_types = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">' + "".join(content_overrides) + "</Types>"

    pres_rels = ['<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="slideMasters/slideMaster1.xml"/>']
    for i in range(1, n + 1):
        pres_rels.append(f'<Relationship Id="rId{i+1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide{i}.xml"/>')

    slide_ids = "".join(f'<p:sldId id="{255+i}" r:id="rId{i+1}"/>' for i in range(1, n + 1))
    presentation = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"
 xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"
 xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
 <p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rId1"/></p:sldMasterIdLst>
 <p:sldIdLst>{slide_ids}</p:sldIdLst>
 <p:sldSz cx="{SLIDE_W}" cy="{SLIDE_H}" type="wide"/>
 <p:notesSz cx="6858000" cy="9144000"/>
</p:presentation>"""

    root_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
 <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="ppt/presentation.xml"/>
 <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
 <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>"""
    pres_rels_xml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' + "".join(pres_rels) + "</Relationships>"

    master = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
 <p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{SLIDE_W}" cy="{SLIDE_H}"/><a:chOff x="0" y="0"/><a:chExt cx="{SLIDE_W}" cy="{SLIDE_H}"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld>
 <p:clrMap bg1="dk1" tx1="lt1" bg2="dk2" tx2="lt2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/>
 <p:sldLayoutIdLst><p:sldLayoutId id="2147483649" r:id="rId1"/></p:sldLayoutIdLst>
</p:sldMaster>"""
    master_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
 <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/>
 <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="../theme/theme1.xml"/>
</Relationships>"""
    layout = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="blank" preserve="1">
 <p:cSld name="Blank"><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{SLIDE_W}" cy="{SLIDE_H}"/><a:chOff x="0" y="0"/><a:chExt cx="{SLIDE_W}" cy="{SLIDE_H}"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld>
 <p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>
</p:sldLayout>"""
    layout_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
 <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="../slideMasters/slideMaster1.xml"/>
</Relationships>"""
    theme = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Astraea">
 <a:themeElements>
  <a:clrScheme name="Astraea"><a:dk1><a:srgbClr val="{COLORS['bg']}"/></a:dk1><a:lt1><a:srgbClr val="{COLORS['lav']}"/></a:lt1><a:dk2><a:srgbClr val="{COLORS['bg2']}"/></a:dk2><a:lt2><a:srgbClr val="{COLORS['white']}"/></a:lt2><a:accent1><a:srgbClr val="{COLORS['teal']}"/></a:accent1><a:accent2><a:srgbClr val="{COLORS['amber']}"/></a:accent2><a:accent3><a:srgbClr val="{COLORS['coral']}"/></a:accent3><a:accent4><a:srgbClr val="{COLORS['indigo']}"/></a:accent4><a:accent5><a:srgbClr val="{COLORS['lav']}"/></a:accent5><a:accent6><a:srgbClr val="{COLORS['muted']}"/></a:accent6><a:hlink><a:srgbClr val="{COLORS['teal']}"/></a:hlink><a:folHlink><a:srgbClr val="{COLORS['indigo']}"/></a:folHlink></a:clrScheme>
  <a:fontScheme name="Aptos"><a:majorFont><a:latin typeface="Aptos Display"/></a:majorFont><a:minorFont><a:latin typeface="Aptos"/></a:minorFont></a:fontScheme>
  <a:fmtScheme name="Astraea"><a:fillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:fillStyleLst><a:lnStyleLst><a:ln w="9525"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln></a:lnStyleLst><a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst><a:bgFillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:bgFillStyleLst></a:fmtScheme>
 </a:themeElements>
</a:theme>"""
    core = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:title>Astraea System Presentation</dc:title><dc:creator>Astraea Project</dc:creator></cp:coreProperties>"""
    app = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"><Application>Astraea</Application><Slides>{n}</Slides></Properties>"""

    with ZipFile(FINAL, "w", ZIP_DEFLATED) as z:
        z.writestr("[Content_Types].xml", content_types)
        z.writestr("_rels/.rels", root_rels)
        z.writestr("docProps/core.xml", core)
        z.writestr("docProps/app.xml", app)
        z.writestr("ppt/presentation.xml", presentation)
        z.writestr("ppt/_rels/presentation.xml.rels", pres_rels_xml)
        z.writestr("ppt/slideMasters/slideMaster1.xml", master)
        z.writestr("ppt/slideMasters/_rels/slideMaster1.xml.rels", master_rels)
        z.writestr("ppt/slideLayouts/slideLayout1.xml", layout)
        z.writestr("ppt/slideLayouts/_rels/slideLayout1.xml.rels", layout_rels)
        z.writestr("ppt/theme/theme1.xml", theme)
        for i, elements in enumerate(slides, 1):
            z.writestr(f"ppt/slides/slide{i}.xml", slide_xml(elements))
            z.writestr(f"ppt/slides/_rels/slide{i}.xml.rels", '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/></Relationships>')


write_package()
print(FINAL)
