namespace PigeonRacing.Infrastructure.Templates;

public static partial class TemplateLibrary
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SUPER ACE PIGEON — 20 TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly (string Id, string Name, string Style, string Html)[] SuperAceTemplates = new[]
    {
        ("SA-01","Super Ace Classic Navy",    "Classic",   BuildSA("Classic","#1E3A5F","#C9A84C","#F0F4F8",false)),
        ("SA-02","Super Ace Gold Landscape",  "Elegant",   BuildSA("Gold","#1a1a1a","#C9A84C","#FAFAF0",true)),
        ("SA-03","Super Ace Green",           "Classic",   BuildSA("Green","#2D6A4F","#F4A261","#F0F7F4",false)),
        ("SA-04","Super Ace Crimson",         "Sporty",    BuildSA("Crimson","#C1121F","#2B2D42","#FFF5F5",false)),
        ("SA-05","Super Ace Ivory Serif",     "Elegant",   BuildSASerif()),
        ("SA-06","Super Ace Dark Mode",       "Dark",      BuildSA("Dark","#0D1B2A","#1E90FF","#0D1B2A",true)),
        ("SA-07","Super Ace Minimal",         "Minimal",   BuildSAMinimal()),
        ("SA-08","Super Ace Purple Royal",    "Elegant",   BuildSA("Purple","#4A0E8F","#D4A017","#F5F0FF",false)),
        ("SA-09","Super Ace Teal Modern",     "Modern",    BuildSA("Teal","#0077B6","#00B4D8","#F0F9FF",false)),
        ("SA-10","Super Ace Branded",         "Branded",   BuildSABranded()),
        ("SA-11","Super Ace Ruby",            "Sporty",    BuildSA("Ruby","#9B1C1C","#FCA5A5","#FFF5F5",false)),
        ("SA-12","Super Ace Slate",           "Corporate", BuildSA("Slate","#475569","#94A3B8","#F8FAFC",false)),
        ("SA-13","Super Ace Ocean Wide",      "Modern",    BuildSA("Ocean","#1E40AF","#93C5FD","#EFF6FF",true)),
        ("SA-14","Super Ace Amber",           "Heritage",  BuildSA("Amber","#92400E","#FCD34D","#FFFBEB",false)),
        ("SA-15","Super Ace Forest",          "Heritage",  BuildSA("Forest","#14532D","#86EFAC","#F0FDF4",false)),
        ("SA-16","Super Ace Rose Gold",       "Elegant",   BuildSA("Rose Gold","#9F1239","#FCA5A1","#FFF1F2",false)),
        ("SA-17","Super Ace Cobalt Landscape","Modern",    BuildSA("Cobalt","#1E3A8A","#93C5FD","#EFF6FF",true)),
        ("SA-18","Super Ace Heritage Brown",  "Heritage",  BuildSA("Heritage","#78350F","#D97706","#FEFCE8",false)),
        ("SA-19","Super Ace Carbon Wide",     "Sporty",    BuildSA("Carbon","#1C1917","#A3E635","#1C1917",true)),
        ("SA-20","Super Ace Lavender",        "Elegant",   BuildSA("Lavender","#4C1D95","#C4B5FD","#F5F3FF",false)),
    };

    private static string BuildSA(string label, string primary, string accent, string bg, bool landscape) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page {{{(landscape ? "size:A4 landscape;" : "")}margin:12mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}} body{{font-family:'Segoe UI',Arial,sans-serif;font-size:10pt;background:{bg};color:#111}}
.header{{background:{primary};color:#fff;padding:14px 20px}}
.header h1{{font-size:18pt;font-weight:800}}.header .star{{font-size:22pt;margin-right:8px;vertical-align:middle}}
.header .sub{{font-size:9.5pt;opacity:.8}}
.elite-badge{{background:{accent};color:{primary};font-weight:900;font-size:10pt;padding:3px 14px;border-radius:999px;display:inline-block;margin:6px 20px;letter-spacing:1px;text-transform:uppercase}}
.qual-note{{font-size:8.5pt;color:#666;margin:0 20px 8px;font-style:italic}}
.prog-meta{{display:flex;gap:20px;background:{accent}22;padding:9px 20px;border-bottom:2px solid {accent};font-size:9pt}}
.prog-meta strong{{color:{primary};display:block}}
thead th{{background:{primary};color:{accent};font-size:9pt;padding:7px 9px;text-align:left}}
thead th:first-child{{width:48px;text-align:center}}
tbody tr:nth-child(even){{background:{accent}11}}
tbody tr:nth-child(1) td{{background:{accent}55;font-weight:700}}
tbody tr:nth-child(2) td{{background:{accent}33}}
tbody tr:nth-child(3) td{{background:{accent}22}}
td{{font-size:9.5pt;border-bottom:1px solid {accent}44;padding:6px 9px}}
.rank{{font-size:14pt;font-weight:900;color:{primary};text-align:center;width:48px}}
.ring{{font-family:monospace;font-size:9.5pt;font-weight:700}}
.score{{font-weight:700;color:{primary}}}
.part{{color:#2D6A4F;font-weight:600}}
.footer{{font-size:8pt;color:#888;text-align:center;padding:7px;margin-top:8px}}
</style></head><body>
<div class='header'><h1><span class='star'>⭐</span>Super Ace Pigeon</h1><div class='sub'>{{{{programme.name}}}} &bull; {{{{club.name}}}} &bull; {label} &bull; Elite Rankings</div></div>
<div class='elite-badge'>⭐ Super Ace Qualified</div>
<div class='qual-note'>Qualification: {{{{programme.superAceQualification}}}}</div>
<div class='prog-meta'>
  <div><strong>Programme</strong>{{{{programme.name}}}}</div>
  <div><strong>Season</strong>{{{{programme.year}}}}</div>
  <div><strong>Races</strong>{{{{programme.raceCount}}}}</div>
  <div><strong>Qualifiers</strong>{{{{totalQualifiers}}}}</div>
  <div><strong>Scoring</strong>{{{{programme.scoringMethod}}}}</div>
</div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Races</th><th>Part%</th><th>Best m/min</th><th>Avg m/min</th><th>Best Rank</th><th>Score</th></tr></thead>
<tbody>{{{{#each results}}}}<tr>
  <td class='rank'>{{{{superAceRank}}}}</td>
  <td class='ring'>{{{{ringNumber}}}}</td>
  <td>{{{{pigeonName}}}}</td>
  <td>{{{{pigeonSex}}}}</td>
  <td>{{{{pigeonYearOfBirth}}}}</td>
  <td>{{{{fancierName}}}}</td>
  <td class='part'>{{{{racesEntered}}}}/{{{{racesInProgramme}}}}</td>
  <td class='part'>{{{{participationRate}}}}%</td>
  <td>{{{{bestVelocityMperMin}}}}</td>
  <td>{{{{averageVelocityMperMin}}}}</td>
  <td>#{{{{bestClubRank}}}}</td>
  <td class='score'>{{{{totalScore}}}}</td>
</tr>{{{{/each}}}}</tbody></table>
<div class='footer'>{{{{club.name}}}} &bull; Super Ace Pigeon {{{{programme.year}}}} &bull; Printed {{{{printDate}}}}</div>
</body></html>";

    private static string BuildSASerif() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:15mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:Georgia,'Times New Roman',serif;font-size:10.5pt;background:#FFFEF8;color:#111}
.border{border:3px double #C9A84C;padding:16px}
.title{text-align:center;border-bottom:2px solid #C9A84C;padding-bottom:14px;margin-bottom:14px}
.title h1{font-size:22pt;color:#1E3A5F;font-weight:800;letter-spacing:1px}
.title .prog{font-size:13pt;color:#C9A84C;font-style:italic;margin-top:4px}
.title .meta{font-size:9.5pt;color:#777;margin-top:6px;text-transform:uppercase;letter-spacing:1px}
.orn{text-align:center;font-size:18pt;color:#C9A84C;margin:4px 0}
.qual{text-align:center;background:#FFF8E1;border:1px solid #C9A84C;border-radius:3px;padding:5px;font-size:9pt;margin-bottom:10px;color:#7D5A00;font-style:italic}
thead th{background:#1E3A5F;color:#C9A84C;font-family:Georgia,serif;font-size:9pt;text-transform:uppercase;letter-spacing:.5px;border:1px solid #C9A84C}
tbody tr:nth-child(even){background:#FBF7EC} td{border:1px solid #E8DFC8;font-size:10pt;padding:6px 8px}
.rank{font-size:14pt;font-weight:700;color:#C9A84C;text-align:center;width:48px}
.ring{font-family:monospace;font-weight:700}.score{font-weight:700;color:#1E3A5F}
.footer{text-align:center;font-size:8.5pt;color:#999;margin-top:12px;font-style:italic}
</style></head><body><div class='border'>
<div class='orn'>&#10022; &#10022; &#10022;</div>
<div class='title'><h1>⭐ Super Ace Pigeon</h1><div class='prog'>{{programme.name}}</div><div class='meta'>{{club.name}} &bull; Season {{programme.year}} &bull; Elite Rankings</div></div>
<div class='orn'>&#8730; &mdash; &#8730;</div>
<div class='qual'>Qualification: {{programme.superAceQualification}}</div>
<table>
<thead><tr><th>#</th><th>Ring</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Races</th><th>Best m/min</th><th>Score</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{superAceRank}}</td><td class='ring'>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYearOfBirth}}</td><td>{{fancierName}}</td><td>{{racesEntered}}/{{racesInProgramme}}</td><td>{{bestVelocityMperMin}}</td><td class='score'>{{totalScore}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &mdash; Super Ace {{programme.year}} &mdash; Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildSAMinimal() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:20mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;color:#111}
.header{padding:20px 0 14px;border-bottom:2px solid #111}
.header h1{font-size:26pt;font-weight:900;letter-spacing:-1px}
.header .sub{font-size:11pt;color:#666;margin-top:3px}
.qual{font-size:9pt;color:#059669;margin:8px 0 4px;font-weight:600}
.meta{display:flex;gap:32px;padding:10px 0;border-bottom:1px solid #DDD;margin-bottom:14px;font-size:8.5pt;color:#666}
.meta strong{display:block;color:#111;font-size:9pt}
thead th{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#666;font-weight:600;border-bottom:2px solid #111;padding:5px 6px;text-align:left}
tbody tr{border-bottom:1px solid #EEE} td{font-size:9.5pt;padding:7px 6px}
.rank{font-size:13pt;font-weight:900;text-align:center;width:44px}
tr:nth-child(1) .rank{color:#D4A017} tr:nth-child(2) .rank{color:#888} tr:nth-child(3) .rank{color:#CD7F32}
.ring{font-family:monospace;font-weight:700}.score{font-weight:700}
.part{color:#059669;font-weight:600}
.footer{font-size:8pt;color:#BBB;margin-top:16px}
</style></head><body>
<div class='header'><h1>⭐ Super Ace Pigeon</h1><div class='sub'>{{programme.name}} &bull; {{club.name}}</div></div>
<div class='qual'>Qualification: {{programme.superAceQualification}}</div>
<div class='meta'>
  <div><strong>Season</strong>{{programme.year}}</div>
  <div><strong>Races</strong>{{programme.raceCount}}</div>
  <div><strong>Qualifiers</strong>{{totalQualifiers}}</div>
  <div><strong>Scoring</strong>{{programme.scoringMethod}}</div>
</div>
<table>
<thead><tr><th>#</th><th>Ring</th><th>Pigeon</th><th>Sex</th><th>Yr</th><th>Fancier</th><th>Races</th><th>Part%</th><th>Best m/min</th><th>Avg m/min</th><th>Best#</th><th>Score</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{superAceRank}}</td><td class='ring'>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYearOfBirth}}</td><td>{{fancierName}}</td><td class='part'>{{racesEntered}}/{{racesInProgramme}}</td><td class='part'>{{participationRate}}%</td><td>{{bestVelocityMperMin}}</td><td>{{averageVelocityMperMin}}</td><td>#{{bestClubRank}}</td><td class='score'>{{totalScore}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &bull; Printed {{printDate}}</div>
</body></html>";

    private static string BuildSABranded() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:12mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Segoe UI',Arial,sans-serif;font-size:10pt;color:#111}
.header{background:{{club.primaryColour}};color:#fff;padding:14px 20px;display:flex;justify-content:space-between;align-items:center}
.header h1{font-size:18pt;font-weight:800}.header .sub{font-size:9pt;opacity:.8}
.logo{width:56px;height:56px;object-fit:contain;background:rgba(255,255,255,.15);border-radius:4px;padding:4px}
.elite{background:{{club.secondaryColour}};color:{{club.primaryColour}};font-weight:900;font-size:9.5pt;padding:3px 14px;border-radius:999px;display:inline-block;margin:6px 20px}
.meta{display:flex;gap:20px;background:{{club.secondaryColour}}22;padding:8px 20px;border-bottom:2px solid {{club.secondaryColour}};font-size:9pt}
.meta strong{color:{{club.primaryColour}};display:block}
thead th{background:{{club.primaryColour}};color:{{club.secondaryColour}};font-size:9pt;padding:7px 9px;text-align:left}
tbody tr:nth-child(even){background:{{club.secondaryColour}}11} td{font-size:9.5pt;border-bottom:1px solid {{club.secondaryColour}}44;padding:6px 9px}
.rank{font-size:14pt;font-weight:900;color:{{club.primaryColour}};text-align:center;width:48px}
.ring{font-family:monospace;font-weight:700}.score{font-weight:700;color:{{club.primaryColour}}}
.part{color:#059669;font-weight:600}
.footer{font-size:8pt;color:#888;text-align:center;padding:7px}
</style></head><body>
<div class='header'><div><h1>⭐ Super Ace Pigeon</h1><div class='sub'>{{programme.name}} &bull; Elite Rankings</div></div><img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'""></div>
<div class='elite'>⭐ Super Ace Qualified — {{programme.superAceQualification}}</div>
<div class='meta'><div><strong>Programme</strong>{{programme.name}}</div><div><strong>Season</strong>{{programme.year}}</div><div><strong>Races</strong>{{programme.raceCount}}</div><div><strong>Qualifiers</strong>{{totalQualifiers}}</div><div><strong>Club</strong>{{club.name}}</div></div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Races</th><th>Part%</th><th>Best m/min</th><th>Score</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{superAceRank}}</td><td class='ring'>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYearOfBirth}}</td><td>{{fancierName}}</td><td class='part'>{{racesEntered}}/{{racesInProgramme}}</td><td class='part'>{{participationRate}}%</td><td>{{bestVelocityMperMin}}</td><td class='score'>{{totalScore}}</td></tr>{{/each}}</tbody>
</table><div class='footer'>{{club.name}} Super Ace Pigeon {{programme.year}} &bull; Printed {{printDate}}</div></body></html>";

    // ═══════════════════════════════════════════════════════════════════════════
    //  CERTIFICATES — 50 TEMPLATES
    //  All certificates are A4 portrait, landscape, or custom formats.
    //  Variables: {{certificate.recipientName}}, {{certificate.rank}},
    //             {{certificate.achievement}}, {{certificate.ringNumber}},
    //             {{certificate.pigeonName}}, {{certificate.velocityMperMin}},
    //             {{certificate.raceName}}, {{race.date}},
    //             {{club.name}}, {{club.logoUrl}}, {{programme.name}}, etc.
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly (string Id, string Name, string Style, string Html)[] CertificateTemplates = new[]
    {
        ("CERT-01", "Classic Navy Winner",           "Classic",   BuildCert01()),
        ("CERT-02", "Gold & Black Champion",         "Elegant",   BuildCert02()),
        ("CERT-03", "Green Federation Cert",         "Classic",   BuildCert03()),
        ("CERT-04", "Crimson Achievement",           "Sporty",    BuildCert04()),
        ("CERT-05", "Royal Ivory & Gold",            "Elegant",   BuildCert05()),
        ("CERT-06", "Dark Prestige",                 "Dark",      BuildCert06()),
        ("CERT-07", "Minimal Clean",                 "Minimal",   BuildCert07()),
        ("CERT-08", "Purple Royal Honour",           "Elegant",   BuildCert08()),
        ("CERT-09", "Teal Modern Award",             "Modern",    BuildCert09()),
        ("CERT-10", "Branded Club Cert",             "Branded",   BuildCert10()),
        ("CERT-11", "Ace Pigeon Certificate",        "Elegant",   BuildCert11()),
        ("CERT-12", "Super Ace Elite Certificate",   "Elegant",   BuildCert12()),
        ("CERT-13", "Best Loft Champion",            "Classic",   BuildCert13()),
        ("CERT-14", "Participant Medal Cert",        "Modern",    BuildCert14()),
        ("CERT-15", "Landscape Navy Blue",           "Classic",   BuildCertLandscape("#1E3A5F","#C9A84C","Winner Certificate")),
        ("CERT-16", "Landscape Gold & Crimson",     "Elegant",   BuildCertLandscape("#C1121F","#D4A017","Champion Certificate")),
        ("CERT-17", "Landscape Green Heritage",     "Heritage",  BuildCertLandscape("#2D6A4F","#F4A261","Achievement Certificate")),
        ("CERT-18", "Landscape Purple Prestige",    "Elegant",   BuildCertLandscape("#4A0E8F","#D4A017","Excellence Award")),
        ("CERT-19", "Landscape Teal Modern",        "Modern",    BuildCertLandscape("#0077B6","#00B4D8","Race Certificate")),
        ("CERT-20", "Landscape Branded",            "Branded",   BuildCertLandscapeBranded()),
        ("CERT-21", "Ruby Red Champion",            "Sporty",    BuildCertSimple("#9B1C1C","#FCA5A5","Champion")),
        ("CERT-22", "Amber Heritage Award",         "Heritage",  BuildCertSimple("#92400E","#FCD34D","Achievement")),
        ("CERT-23", "Forest Deep Honour",           "Classic",   BuildCertSimple("#14532D","#86EFAC","Honours")),
        ("CERT-24", "Rose Gold Prestige",           "Elegant",   BuildCertSimple("#9F1239","#FCA5A1","Prestige Award")),
        ("CERT-25", "Cobalt Winner",                "Modern",    BuildCertSimple("#1E3A8A","#93C5FD","Winner")),
        ("CERT-26", "Maroon Heritage Cert",         "Heritage",  BuildCertSimple("#4C1130","#F9A8D4","Heritage Award")),
        ("CERT-27", "Steel Corporate Award",        "Corporate", BuildCertSimple("#374151","#D1D5DB","Corporate Award")),
        ("CERT-28", "Sunset Orange Honour",         "Sporty",    BuildCertSimple("#C2410C","#FDBA74","Honour")),
        ("CERT-29", "Sky Blue Achievement",         "Vibrant",   BuildCertSimple("#075985","#BAE6FD","Achievement")),
        ("CERT-30", "Platinum Minimal Award",       "Minimal",   BuildCertSimple("#1F2937","#E5E7EB","Award")),
        ("CERT-31", "Pigeon Champion Detailed",     "Classic",   BuildCertPigeon()),
        ("CERT-32", "Pigeon Ace Detailed",          "Elegant",   BuildCertPigeonAce()),
        ("CERT-33", "Season Champion",              "Elegant",   BuildCertSeason()),
        ("CERT-34", "First Place Podium",           "Modern",    BuildCertPodium("1st","🥇","#D4A017","#FFF8E1")),
        ("CERT-35", "Second Place Podium",          "Modern",    BuildCertPodium("2nd","🥈","#888888","#F5F5F5")),
        ("CERT-36", "Third Place Podium",           "Modern",    BuildCertPodium("3rd","🥉","#CD7F32","#FFF3E0")),
        ("CERT-37", "Participation Certificate",    "Classic",   BuildCertParticipation()),
        ("CERT-38", "Club Membership Cert",         "Corporate", BuildCertMembership()),
        ("CERT-39", "Velocity Record Cert",         "Sporty",    BuildCertVelocity()),
        ("CERT-40", "Distance Record Cert",         "Sporty",    BuildCertDistance()),
        ("CERT-41", "Multi-Race Champion Landscape","Elegant",   BuildCertMultiRaceLandscape()),
        ("CERT-42", "Vintage Ornate Classic",       "Heritage",  BuildCertVintage()),
        ("CERT-43", "Watercolour Wash",             "Vibrant",   BuildCertWash("#EFF6FF","#1E3A8A","#C9A84C")),
        ("CERT-44", "Forest Green Wash",            "Vibrant",   BuildCertWash("#F0FDF4","#14532D","#86EFAC")),
        ("CERT-45", "Crimson Wash",                 "Vibrant",   BuildCertWash("#FFF1F2","#9B1C1C","#FCA5A5")),
        ("CERT-46", "Branded Landscape Champion",   "Branded",   BuildCertBrandedLandscape()),
        ("CERT-47", "Young Bird Champion",          "Modern",    BuildCertYoungBird()),
        ("CERT-48", "Old Bird Champion",            "Heritage",  BuildCertOldBird()),
        ("CERT-49", "National Result Certificate",  "Elegant",   BuildCertNational()),
        ("CERT-50", "Grand Prix Champion",          "Elegant",   BuildCertGrandPrix()),
    };

    // ── Certificate builder helpers ───────────────────────────────────────────

    private static string CertBase(string primary, string accent, string bg, string borderStyle = "solid") => $@"
@page{{margin:14mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}}
body{{font-family:'Segoe UI',Arial,sans-serif;background:{bg};color:#111}}
.outer{{border:4px {borderStyle} {accent};padding:20px;min-height:93vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center}}
.logo{{width:70px;height:70px;object-fit:contain;margin-bottom:12px}}
.club-name{{font-size:13pt;color:{primary};font-weight:700;letter-spacing:1px;text-transform:uppercase;margin-bottom:6px}}
.title{{font-size:32pt;font-weight:900;color:{primary};letter-spacing:2px;text-transform:uppercase;line-height:1.1;margin:10px 0}}
.subtitle{{font-size:14pt;color:{accent};font-weight:600;margin-bottom:20px;font-style:italic}}
.recipient{{font-size:26pt;font-weight:900;color:{primary};border-bottom:3px solid {accent};border-top:3px solid {accent};padding:12px 40px;margin:14px 0;letter-spacing:1px}}
.achievement{{font-size:13pt;color:#444;max-width:480px;line-height:1.5;margin:8px auto}}
.pigeon-ring{{font-family:monospace;font-size:14pt;font-weight:700;color:{primary};background:{accent}22;border:1px solid {accent};border-radius:4px;padding:4px 16px;display:inline-block;margin:6px 0}}
.stats{{display:flex;gap:24px;justify-content:center;margin:12px 0;flex-wrap:wrap}}
.stat-item{{text-align:center;padding:8px 16px;background:{accent}22;border-radius:4px;min-width:90px}}
.stat-item .value{{font-size:14pt;font-weight:900;color:{primary};display:block}}
.stat-item .label{{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#777}}
.divider{{width:200px;height:2px;background:linear-gradient(90deg,transparent,{accent},{accent},transparent);margin:10px auto}}
.seal{{font-size:48pt;margin:8px 0}}
.season{{font-size:10pt;color:#888;text-transform:uppercase;letter-spacing:2px;margin-top:8px}}
.signatures{{display:flex;gap:60px;justify-content:center;margin-top:20px}}
.sig{{text-align:center;width:130px}}
.sig .line{{border-top:1px solid #999;margin-bottom:4px;height:30px}}
.sig .role{{font-size:8pt;color:#777;text-transform:uppercase;letter-spacing:1px}}
.footer{{font-size:8pt;color:#BBB;margin-top:12px}}
";

    private static string BuildCert01() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#1E3A5F","#C9A84C","#FAFBFC")}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{{{club.logoUrl}}}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{{{club.name}}}}</div>
<div class='divider'></div>
<div class='title'>Certificate of Achievement</div>
<div class='subtitle'>This is to certify that</div>
<div class='recipient'>{{{{certificate.recipientName}}}}</div>
<div class='achievement'>has achieved <strong>{{{{certificate.rank}}}}</strong> place in<br><strong>{{{{certificate.raceName}}}}</strong></div>
<div style='margin:10px 0'><span class='pigeon-ring'>{{{{certificate.ringNumber}}}}</span></div>
{{{{#if certificate.pigeonName}}}}<div style='font-size:11pt;color:#555;margin-bottom:8px'>Pigeon: <strong>{{{{certificate.pigeonName}}}}</strong></div>{{{{/if}}}}
<div class='stats'>
  <div class='stat-item'><span class='value'>{{{{certificate.velocityMperMin}}}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{{{certificate.distanceKm}}}}</span><span class='label'>km</span></div>
  <div class='stat-item'><span class='value'>{{{{certificate.arrivalTime}}}}</span><span class='label'>Arrival</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{{{club.name}}}} &bull; Season {{{{season}}}} &bull; {{{{race.date}}}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{{{printDate}}}}</div>
</div></body></html>";

    private static string BuildCert02() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#1a1a1a","#C9A84C","#0D0D0D","double")}
.outer{{background:#0D0D0D;color:#E8E0CC}}
.title{{color:#C9A84C}} .recipient{{color:#C9A84C}} .club-name{{color:#C9A84C}}
.achievement{{color:#B0A890}} .season{{color:#666}}
.stat-item .value{{color:#C9A84C}}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='seal'>🏆</div>
<div class='club-name'>{{club.name}}</div>
<div class='title'>Champion Certificate</div>
<div class='subtitle' style='color:#C9A84C'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='achievement'>Achieved <strong style='color:#C9A84C'>{{certificate.rank}}</strong> place in <strong style='color:#C9A84C'>{{certificate.raceName}}</strong></div>
<div style='margin:10px 0'><span class='pigeon-ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.velocityMperMin}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{certificate.distanceKm}}</span><span class='label'>km</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{club.name}} &bull; Season {{season}}</div>
<div class='signatures'>
  <div class='sig'><div class='line' style='border-color:#555'></div><div class='role' style='color:#666'>Club Manager</div></div>
  <div class='sig'><div class='line' style='border-color:#555'></div><div class='role' style='color:#666'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCert03() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#2D6A4F","#F4A261","#F0F7F4")}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Race Certificate</div>
<div class='subtitle'>Presented to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='achievement'>for achieving position <strong>{{certificate.rank}}</strong> in<br><strong>{{certificate.raceName}}</strong></div>
<div style='margin:10px 0'><span class='pigeon-ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.velocityMperMin}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{certificate.distanceKm}}</span><span class='label'>km</span></div>
  <div class='stat-item'><span class='value'>{{race.date}}</span><span class='label'>Date</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{club.name}} &bull; {{season}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCert04() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#C1121F","#2B2D42","#FFF5F5")}
.title{{font-size:28pt;letter-spacing:3px}}
.seal{{font-size:52pt}}
</style></head><body>
<div class='outer'>
<div class='seal'>🏁</div>
<div class='club-name'>{{club.name}}</div>
<div class='title'>Achievement Award</div>
<div class='subtitle'>Congratulates</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='achievement'>on finishing <strong>{{certificate.rank}}</strong> in <strong>{{certificate.raceName}}</strong></div>
<div style='margin:10px 0'><span class='pigeon-ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.velocityMperMin}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{certificate.distanceKm}}</span><span class='label'>km</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{club.name}} &bull; Season {{season}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Race Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCert05() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{{margin:14mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}}
body{{font-family:Georgia,'Times New Roman',serif;background:#FFFEF8;color:#111}}
.outer{{border:5px double #C9A84C;padding:24px;min-height:93vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center}}
.inner{{border:1px solid #C9A84C;padding:18px;width:100%}}
.orn{{font-size:18pt;color:#C9A84C;margin:4px 0}}
.logo{{width:65px;height:65px;object-fit:contain;margin-bottom:8px}}
.club-name{{font-size:12pt;color:#C9A84C;font-weight:700;text-transform:uppercase;letter-spacing:2px}}
.title{{font-size:28pt;font-weight:700;color:#1E3A5F;letter-spacing:2px;text-transform:uppercase;margin:10px 0}}
.presented{{font-size:11pt;font-style:italic;color:#777;margin:4px 0}}
.recipient{{font-size:28pt;font-weight:700;color:#C9A84C;margin:8px 0;font-family:Georgia,serif}}
.for-line{{font-size:11pt;color:#555;margin:6px 0}}
.race{{font-size:15pt;font-weight:700;color:#1E3A5F;margin:4px 0}}
.ring{{font-family:monospace;font-size:13pt;font-weight:700;color:#1E3A5F;background:#FFF8E1;border:1px solid #C9A84C;border-radius:3px;padding:3px 14px;display:inline-block;margin:6px 0}}
.stats{{display:flex;gap:20px;justify-content:center;margin:12px 0}}
.stat-item{{padding:7px 14px;background:#FFF8E1;border:1px solid #C9A84C;border-radius:3px;text-align:center}}
.stat-item .value{{font-size:13pt;font-weight:700;color:#1E3A5F;display:block}}
.stat-item .label{{font-size:7.5pt;text-transform:uppercase;letter-spacing:1px;color:#888}}
.divider{{width:200px;height:1px;background:#C9A84C;margin:10px auto}}
.date{{font-size:10pt;color:#888;text-transform:uppercase;letter-spacing:1px}}
.sigs{{display:flex;gap:60px;justify-content:center;margin-top:18px}}
.sig{{width:130px;text-align:center}}
.sig .line{{border-top:1px solid #C9A84C;margin-bottom:4px;height:28px}}
.sig .role{{font-size:8pt;color:#888;text-transform:uppercase;letter-spacing:1px}}
.footer{{font-size:8pt;color:#CCC;margin-top:8px}}
</style></head><body>
<div class='outer'><div class='inner'>
<div class='orn'>&#10022; &#10022; &#10022;</div>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Certificate of Excellence</div>
<div class='presented'>This certificate is presented to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='for-line'>for achieving <strong>{{certificate.rank}}</strong> place in</div>
<div class='race'>{{certificate.raceName}}</div>
<div style='margin:8px 0'><span class='ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.velocityMperMin}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{certificate.distanceKm}}</span><span class='label'>km</span></div>
  <div class='stat-item'><span class='value'>{{certificate.arrivalTime}}</span><span class='label'>Arrival</span></div>
</div>
<div class='divider'></div>
<div class='date'>{{club.name}} &bull; Season {{season}} &bull; {{race.date}}</div>
<div class='sigs'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='orn' style='margin-top:8px'>&#8730; &mdash; &#8730;</div>
<div class='footer'>Printed {{printDate}}</div>
</div></div></body></html>";

    private static string BuildCert06() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#0D1B2A","#1E90FF","#0A1520","double")}
.outer{{background:linear-gradient(135deg,#0A1520 0%,#132030 100%);color:#E8F0FE}}
.title{{color:#1E90FF;text-shadow:0 0 20px rgba(30,144,255,.4)}}
.recipient{{color:#E8F0FE;border-color:#1E90FF}}
.club-name{{color:#1E90FF}} .subtitle{{color:#8FA8C8}}
.achievement{{color:#8FA8C8}} .season{{color:#4A6080}}
.stat-item{{background:rgba(30,144,255,.1);border:1px solid rgba(30,144,255,.3)}}
.stat-item .value{{color:#1E90FF}} .stat-item .label{{color:#8FA8C8}}
.sig .line{{border-color:#1E3A5F}} .sig .role{{color:#4A6080}}
.footer{{color:#2A3A50}}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='seal' style='filter:brightness(1.5)'>🏆</div>
<div class='club-name'>{{club.name}}</div>
<div class='title'>Prestige Certificate</div>
<div class='subtitle'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='achievement'>for achieving <strong style='color:#1E90FF'>{{certificate.rank}}</strong> in<br><strong style='color:#1E90FF'>{{certificate.raceName}}</strong></div>
<div style='margin:10px 0'><span class='pigeon-ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.velocityMperMin}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{certificate.distanceKm}}</span><span class='label'>km</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{club.name}} &bull; Season {{season}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    // Certs 07-50 use the simple/landscape/special builders
    private static string BuildCert07() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{{margin:20mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}} body{{font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;background:#fff;color:#111}}
.outer{{min-height:90vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;border-top:3px solid #111;border-bottom:3px solid #111;padding:24px 0}}
.logo{{width:50px;height:50px;object-fit:contain;margin-bottom:10px}}
.club-name{{font-size:10pt;text-transform:uppercase;letter-spacing:3px;color:#666;margin-bottom:8px}}
.title{{font-size:30pt;font-weight:900;letter-spacing:-1px;color:#111;margin:8px 0}}
.sub{{font-size:11pt;color:#777;font-style:italic;margin:6px 0}}
.recipient{{font-size:28pt;font-weight:900;color:#111;border-bottom:1px solid #111;padding:10px 0;margin:10px 40px}}
.ach{{font-size:12pt;color:#555;margin:8px 0}}
.ring{{font-family:monospace;font-size:12pt;font-weight:700;background:#F5F5F5;padding:3px 12px;border-radius:3px;display:inline-block;margin:6px 0}}
.stats{{display:flex;gap:20px;justify-content:center;margin:12px 0;flex-wrap:wrap}}
.s{{text-align:center;min-width:80px}}
.s .val{{font-size:14pt;font-weight:900;display:block}}
.s .lbl{{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#999}}
.div{{width:120px;height:1px;background:#111;margin:10px auto}}
.dt{{font-size:9pt;color:#999;text-transform:uppercase;letter-spacing:1px}}
.sigs{{display:flex;gap:60px;justify-content:center;margin-top:20px}}
.sig{{width:120px;text-align:center}}
.sig .line{{border-top:1px solid #CCC;margin-bottom:4px;height:24px}}
.sig .role{{font-size:8pt;color:#999;text-transform:uppercase;letter-spacing:1px}}
.foot{{font-size:8pt;color:#DDD;margin-top:10px}}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Certificate</div>
<div class='sub'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='ach'>for <strong>{{certificate.rank}}</strong> place in <strong>{{certificate.raceName}}</strong></div>
<div style='margin:8px 0'><span class='ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='s'><span class='val'>{{certificate.velocityMperMin}}</span><span class='lbl'>m/min</span></div>
  <div class='s'><span class='val'>{{certificate.distanceKm}}</span><span class='lbl'>km</span></div>
  <div class='s'><span class='val'>{{race.date}}</span><span class='lbl'>Date</span></div>
</div>
<div class='div'></div>
<div class='dt'>{{club.name}} &bull; Season {{season}}</div>
<div class='sigs'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='foot'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCert08() => BuildCertSimple("#4A0E8F","#D4A017","Royal Honours");
    private static string BuildCert09() => BuildCertSimple("#0077B6","#00B4D8","Modern Award");
    private static string BuildCert10() => BuildCertBranded();

    private static string BuildCert11() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#1E3A5F","#C9A84C","#F0F4F8")}
</style></head><body>
<div class='outer'>
<div class='seal'>🕊️</div>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Ace Pigeon Certificate</div>
<div class='subtitle'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='achievement'>whose pigeon <strong>{{certificate.pigeonName}}</strong> achieved<br>Ace Pigeon status in <strong>{{programme.name}}</strong></div>
<div style='margin:10px 0'><span class='pigeon-ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.aceRank}}</span><span class='label'>Ace Rank</span></div>
  <div class='stat-item'><span class='value'>{{certificate.totalScore}}</span><span class='label'>Total Score</span></div>
  <div class='stat-item'><span class='value'>{{certificate.racesEntered}}</span><span class='label'>Races</span></div>
  <div class='stat-item'><span class='value'>{{certificate.bestVelocityMperMin}}</span><span class='label'>Best m/min</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{club.name}} &bull; Programme: {{programme.name}} &bull; {{programme.year}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCert12() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#1E3A5F","#C9A84C","#F0F4F8")}
</style></head><body>
<div class='outer'>
<div class='seal'>⭐</div>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Super Ace Pigeon</div>
<div class='subtitle' style='color:#C9A84C;font-size:16pt'>Elite Certificate</div>
<div class='presented' style='font-size:11pt;color:#777;font-style:italic;margin:8px 0'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='achievement'>whose pigeon <strong>{{certificate.pigeonName}}</strong> qualified as<br>Super Ace Pigeon in <strong>{{programme.name}}</strong></div>
<div style='margin:10px 0'><span class='pigeon-ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.superAceRank}}</span><span class='label'>Super Ace Rank</span></div>
  <div class='stat-item'><span class='value'>{{certificate.totalScore}}</span><span class='label'>Total Score</span></div>
  <div class='stat-item'><span class='value'>{{certificate.racesEntered}}/{{certificate.racesInProgramme}}</span><span class='label'>All Races</span></div>
  <div class='stat-item'><span class='value'>{{certificate.bestVelocityMperMin}}</span><span class='label'>Best m/min</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{club.name}} &bull; {{programme.name}} &bull; {{programme.year}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCert13() => $@"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase("#1E3A5F","#C9A84C","#F0F4F8")}
</style></head><body>
<div class='outer'>
<div class='seal'>🏠</div>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Best Loft Champion</div>
<div class='subtitle'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='achievement'>for achieving Best Loft <strong>{{certificate.rank}}</strong> in<br><strong>{{programme.name}}</strong></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.loftRank}}</span><span class='label'>Loft Rank</span></div>
  <div class='stat-item'><span class='value'>{{certificate.totalScore}}</span><span class='label'>Total Score</span></div>
  <div class='stat-item'><span class='value'>{{certificate.racesEntered}}</span><span class='label'>Races</span></div>
  <div class='stat-item'><span class='value'>{{certificate.pigeonsEntered}}</span><span class='label'>Pigeons</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{club.name}} &bull; {{programme.name}} &bull; {{programme.year}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCert14() => BuildCertSimple("#2D6A4F","#F4A261","Participation");

    private static string BuildCertSimple(string primary, string accent, string label) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{CertBase(primary, accent, "#FAFAF9")}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{{{club.logoUrl}}}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{{{club.name}}}}</div>
<div class='title'>{label}</div>
<div class='subtitle'>Presented to</div>
<div class='recipient'>{{{{certificate.recipientName}}}}</div>
<div class='achievement'>for <strong>{{{{certificate.rank}}}}</strong> place in <strong>{{{{certificate.raceName}}}}</strong></div>
<div style='margin:8px 0'><span class='pigeon-ring'>{{{{certificate.ringNumber}}}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{{{certificate.velocityMperMin}}}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{{{certificate.distanceKm}}}}</span><span class='label'>km</span></div>
</div>
<div class='divider'></div>
<div class='season'>{{{{club.name}}}} &bull; Season {{{{season}}}}</div>
<div class='signatures'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{{{printDate}}}}</div>
</div></body></html>";

    private static string BuildCertLandscape(string primary, string accent, string label) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{{size:A4 landscape;margin:14mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}}
body{{font-family:'Segoe UI',Arial,sans-serif;background:#FAFAF9;color:#111}}
.outer{{border:4px solid {accent};padding:20px;min-height:90vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center}}
.logo{{width:60px;height:60px;object-fit:contain;margin-bottom:8px}}
.club-name{{font-size:11pt;color:{primary};font-weight:700;text-transform:uppercase;letter-spacing:2px}}
.title{{font-size:36pt;font-weight:900;color:{primary};letter-spacing:2px;text-transform:uppercase;margin:8px 0}}
.subtitle{{font-size:12pt;color:{accent};font-weight:600;font-style:italic}}
.recipient{{font-size:30pt;font-weight:900;color:{primary};border-bottom:3px solid {accent};border-top:3px solid {accent};padding:10px 50px;margin:12px 0}}
.ach{{font-size:12pt;color:#555;margin:6px 0}}
.ring{{font-family:monospace;font-size:13pt;font-weight:700;background:{accent}22;border:1px solid {accent};border-radius:4px;padding:3px 14px;display:inline-block;margin:4px 0}}
.stats{{display:flex;gap:20px;justify-content:center;margin:10px 0;flex-wrap:wrap}}
.stat-item{{text-align:center;padding:7px 14px;background:{accent}22;border-radius:4px;min-width:90px}}
.stat-item .value{{font-size:14pt;font-weight:900;color:{primary};display:block}}
.stat-item .label{{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#777}}
.div{{width:200px;height:2px;background:linear-gradient(90deg,transparent,{accent},{accent},transparent);margin:8px auto}}
.season{{font-size:9pt;color:#888;text-transform:uppercase;letter-spacing:2px}}
.sigs{{display:flex;gap:80px;justify-content:center;margin-top:16px}}
.sig{{width:130px;text-align:center}}
.sig .line{{border-top:1px solid #CCC;margin-bottom:4px;height:26px}}
.sig .role{{font-size:8pt;color:#999;text-transform:uppercase;letter-spacing:1px}}
.footer{{font-size:8pt;color:#CCC;margin-top:8px}}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{{{club.logoUrl}}}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{{{club.name}}}}</div>
<div class='title'>{label}</div>
<div class='subtitle'>Awarded to</div>
<div class='recipient'>{{{{certificate.recipientName}}}}</div>
<div class='ach'>for <strong>{{{{certificate.rank}}}}</strong> place in <strong>{{{{certificate.raceName}}}}</strong></div>
<div style='margin:8px 0'><span class='ring'>{{{{certificate.ringNumber}}}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{{{certificate.velocityMperMin}}}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{{{certificate.distanceKm}}}}</span><span class='label'>km</span></div>
  <div class='stat-item'><span class='value'>{{{{race.date}}}}</span><span class='label'>Date</span></div>
</div>
<div class='div'></div>
<div class='season'>{{{{club.name}}}} &bull; Season {{{{season}}}}</div>
<div class='sigs'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{{{printDate}}}}</div>
</div></body></html>";

    private static string BuildCertLandscapeBranded() => @"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{size:A4 landscape;margin:14mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',Arial,sans-serif;background:#FAFAF9;color:#111}
.outer{border:4px solid {{club.secondaryColour}};padding:20px;min-height:90vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center}
.logo{width:60px;height:60px;object-fit:contain;margin-bottom:8px}
.club-name{font-size:11pt;color:{{club.primaryColour}};font-weight:700;text-transform:uppercase;letter-spacing:2px}
.title{font-size:34pt;font-weight:900;color:{{club.primaryColour}};letter-spacing:2px;text-transform:uppercase;margin:8px 0}
.subtitle{font-size:12pt;color:{{club.secondaryColour}};font-weight:600;font-style:italic}
.recipient{font-size:28pt;font-weight:900;color:{{club.primaryColour}};border-bottom:3px solid {{club.secondaryColour}};border-top:3px solid {{club.secondaryColour}};padding:10px 50px;margin:12px 0}
.ach{font-size:12pt;color:#555;margin:6px 0}
.ring{font-family:monospace;font-size:13pt;font-weight:700;background:{{club.secondaryColour}}22;border:1px solid {{club.secondaryColour}};border-radius:4px;padding:3px 14px;display:inline-block;margin:4px 0}
.stats{display:flex;gap:20px;justify-content:center;margin:10px 0}
.stat-item{text-align:center;padding:7px 14px;background:{{club.secondaryColour}}22;border-radius:4px;min-width:90px}
.stat-item .value{font-size:14pt;font-weight:900;color:{{club.primaryColour}};display:block}
.stat-item .label{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#777}
.sigs{display:flex;gap:80px;justify-content:center;margin-top:16px}
.sig{width:130px;text-align:center}
.sig .line{border-top:1px solid #CCC;margin-bottom:4px;height:26px}
.sig .role{font-size:8pt;color:#999;text-transform:uppercase;letter-spacing:1px}
.footer{font-size:8pt;color:#CCC;margin-top:8px}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Champion Certificate</div>
<div class='subtitle'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='ach'>for <strong>{{certificate.rank}}</strong> place in <strong>{{certificate.raceName}}</strong></div>
<div style='margin:8px 0'><span class='ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.velocityMperMin}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{certificate.distanceKm}}</span><span class='label'>km</span></div>
  <div class='stat-item'><span class='value'>{{race.date}}</span><span class='label'>Date</span></div>
</div>
<div class='sigs'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCertBranded() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:14mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',Arial,sans-serif;background:#FAFAF9;color:#111}
.outer{border:4px solid {{club.secondaryColour}};padding:20px;min-height:93vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center}
.logo{width:70px;height:70px;object-fit:contain;margin-bottom:10px}
.club-name{font-size:13pt;color:{{club.primaryColour}};font-weight:700;text-transform:uppercase;letter-spacing:2px}
.title{font-size:30pt;font-weight:900;color:{{club.primaryColour}};letter-spacing:2px;text-transform:uppercase;margin:10px 0}
.subtitle{font-size:13pt;color:{{club.secondaryColour}};font-weight:600;font-style:italic}
.recipient{font-size:26pt;font-weight:900;color:{{club.primaryColour}};border-bottom:3px solid {{club.secondaryColour}};border-top:3px solid {{club.secondaryColour}};padding:12px 40px;margin:14px 0}
.ach{font-size:12pt;color:#444;margin:8px 0}
.ring{font-family:monospace;font-size:13pt;font-weight:700;background:{{club.secondaryColour}}22;border:1px solid {{club.secondaryColour}};border-radius:4px;padding:4px 16px;display:inline-block;margin:6px 0}
.stats{display:flex;gap:24px;justify-content:center;margin:12px 0}
.stat-item{text-align:center;padding:8px 16px;background:{{club.secondaryColour}}22;border-radius:4px;min-width:90px}
.stat-item .value{font-size:14pt;font-weight:900;color:{{club.primaryColour}};display:block}
.stat-item .label{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#777}
.div{width:200px;height:2px;background:linear-gradient(90deg,transparent,{{club.secondaryColour}},transparent);margin:10px auto}
.season{font-size:10pt;color:#888;text-transform:uppercase;letter-spacing:2px;margin-top:8px}
.sigs{display:flex;gap:60px;justify-content:center;margin-top:20px}
.sig{width:130px;text-align:center}
.sig .line{border-top:1px solid #CCC;margin-bottom:4px;height:30px}
.sig .role{font-size:8pt;color:#999;text-transform:uppercase;letter-spacing:1px}
.footer{font-size:8pt;color:#CCC;margin-top:12px}
</style></head><body>
<div class='outer'>
<img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{club.name}}</div>
<div class='title'>Certificate of Achievement</div>
<div class='subtitle'>Awarded to</div>
<div class='recipient'>{{certificate.recipientName}}</div>
<div class='ach'>for <strong>{{certificate.rank}}</strong> place in <strong>{{certificate.raceName}}</strong></div>
<div style='margin:10px 0'><span class='ring'>{{certificate.ringNumber}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{certificate.velocityMperMin}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{certificate.distanceKm}}</span><span class='label'>km</span></div>
  <div class='stat-item'><span class='value'>{{race.date}}</span><span class='label'>Date</span></div>
</div>
<div class='div'></div>
<div class='season'>{{club.name}} &bull; Season {{season}}</div>
<div class='sigs'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildCertPodium(string place, string medal, string colour, string bg) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{{margin:14mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}}
body{{font-family:'Segoe UI',Arial,sans-serif;background:{bg};color:#111}}
.outer{{border:4px solid {colour};padding:20px;min-height:93vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center}}
.medal{{font-size:72pt;margin-bottom:4px;line-height:1}}
.place{{font-size:48pt;font-weight:900;color:{colour};margin:4px 0;letter-spacing:2px}}
.logo{{width:56px;height:56px;object-fit:contain;margin:8px 0}}
.club-name{{font-size:11pt;color:{colour};font-weight:700;text-transform:uppercase;letter-spacing:2px}}
.title{{font-size:24pt;font-weight:900;color:#111;letter-spacing:1px;margin:8px 0}}
.sub{{font-size:11pt;color:#777;font-style:italic}}
.recipient{{font-size:26pt;font-weight:900;color:#111;border-bottom:3px solid {colour};border-top:3px solid {colour};padding:10px 40px;margin:12px 0}}
.ach{{font-size:11pt;color:#555;margin:6px 0}}
.ring{{font-family:monospace;font-size:12pt;font-weight:700;background:{colour}22;border:1px solid {colour};border-radius:4px;padding:3px 12px;display:inline-block;margin:6px 0}}
.stats{{display:flex;gap:20px;justify-content:center;margin:10px 0}}
.stat-item{{text-align:center;padding:8px 14px;background:{colour}22;border-radius:4px}}
.stat-item .value{{font-size:13pt;font-weight:900;color:{colour};display:block}}
.stat-item .label{{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#777}}
.sigs{{display:flex;gap:60px;justify-content:center;margin-top:18px}}
.sig{{width:120px;text-align:center}}
.sig .line{{border-top:1px solid #CCC;margin-bottom:4px;height:26px}}
.sig .role{{font-size:8pt;color:#999;text-transform:uppercase;letter-spacing:1px}}
.footer{{font-size:8pt;color:#BBB;margin-top:10px}}
</style></head><body>
<div class='outer'>
<div class='medal'>{medal}</div>
<div class='place'>{place}</div>
<img class='logo' src='{{{{club.logoUrl}}}}' onerror=""this.style.display='none'"">
<div class='club-name'>{{{{club.name}}}}</div>
<div class='title'>{place} Place Certificate</div>
<div class='sub'>Awarded to</div>
<div class='recipient'>{{{{certificate.recipientName}}}}</div>
<div class='ach'>in <strong>{{{{certificate.raceName}}}}</strong></div>
<div style='margin:8px 0'><span class='ring'>{{{{certificate.ringNumber}}}}</span></div>
<div class='stats'>
  <div class='stat-item'><span class='value'>{{{{certificate.velocityMperMin}}}}</span><span class='label'>m/min</span></div>
  <div class='stat-item'><span class='value'>{{{{certificate.distanceKm}}}}</span><span class='label'>km</span></div>
  <div class='stat-item'><span class='value'>{{{{race.date}}}}</span><span class='label'>Date</span></div>
</div>
<div class='sigs'>
  <div class='sig'><div class='line'></div><div class='role'>Club Manager</div></div>
  <div class='sig'><div class='line'></div><div class='role'>Secretary</div></div>
</div>
<div class='footer'>{{{{club.name}}}} &bull; Season {{{{season}}}} &bull; Printed {{{{printDate}}}}</div>
</div></body></html>";

    private static string BuildCertPigeon() => BuildCert01(); // pigeon champion reuses cert01 with pigeon fields
    private static string BuildCertPigeonAce() => BuildCert11();
    private static string BuildCertSeason() => BuildCert13();
    private static string BuildCertParticipation() => BuildCert14();
    private static string BuildCertMembership() => BuildCertSimple("#374151","#D1D5DB","Membership Certificate");
    private static string BuildCertVelocity() => BuildCertSimple("#C2410C","#FDBA74","Velocity Record");
    private static string BuildCertDistance() => BuildCertSimple("#1E40AF","#93C5FD","Distance Record");
    private static string BuildCertMultiRaceLandscape() => BuildCertLandscape("#1E3A5F","#C9A84C","Season Champion");
    private static string BuildCertVintage() => BuildCert05();
    private static string BuildCertWash(string bg, string primary, string accent) => BuildCertSimple(primary, accent, "Achievement Award");
    private static string BuildCertBrandedLandscape() => BuildCertLandscapeBranded();
    private static string BuildCertYoungBird() => BuildCertSimple("#2D6A4F","#86EFAC","Young Bird Champion");
    private static string BuildCertOldBird() => BuildCertSimple("#78350F","#D97706","Old Bird Champion");
    private static string BuildCertNational() => BuildCertSimple("#1E3A5F","#C9A84C","National Certificate");
    private static string BuildCertGrandPrix() => BuildCert02();
}
